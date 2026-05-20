using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SkiRental.Logic;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using SkiRental.App.Services;
using ZXing.Net.Maui.Controls;

namespace SkiRental.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Montserrat-Bold.ttf",   "MontserratBold");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        // ── Configuration ─────────────────────────────────────────────────────
        // appsettings.json must be EmbeddedResource in the .csproj.
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SkiRental.App.appsettings.json");
        if (stream is not null)
            builder.Configuration.AddJsonStream(stream);

        var config = builder.Configuration;

        // ── Database ──────────────────────────────────────────────────────────
        var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "SkiRental.db");
        var connectionString = $"Data Source={localDbPath}";

        builder.Services.AddSkiRentalServices(connectionString, configuration: config);

        // ── JWT ───────────────────────────────────────────────────────────────
        builder.Services.Configure<JwtOptions>(opts =>
        {
            var s = config.GetSection(JwtOptions.SectionName);
            opts.SecretKey         = s["SecretKey"] is { Length: > 10 } k
                                       ? k
                                       : "SkiRental_DevKey_ChangeInProduction_32chars!";
            opts.Issuer            = s["Issuer"]   ?? "https://skirent.app";
            opts.Audience          = s["Audience"] ?? "SkiRental.App";
            opts.ExpirationMinutes = int.TryParse(s["ExpirationMinutes"], out var e) ? e : 60;
            opts.ClockSkewMinutes  = int.TryParse(s["ClockSkewMinutes"],  out var c) ? c : 5;
        });

        // ── PayU ──────────────────────────────────────────────────────────────
        builder.Services.Configure<PayUOptions>(config.GetSection(PayUOptions.SectionName));

        // ── HTTP clients ──────────────────────────────────────────────────────
        // IMPORTANT: AddHttpClient<T> registers T as a transient typed-client service
        // that receives a properly configured HttpClient from IHttpClientFactory.
        // Never add a second AddScoped/AddSingleton for the same type — the second
        // registration bypasses the factory pipeline, leaving HttpClient un-injected
        // and causing an InvalidOperationException at first resolution.

        // PayU typed client (transient via IHttpClientFactory).
        // IPaymentProvider is exposed as Scoped by building the instance manually
        // so each Blazor scope gets a fresh PayUPaymentProvider with a pooled HttpClient.
        builder.Services.AddHttpClient(nameof(PayUPaymentProvider), client =>
            client.Timeout = TimeSpan.FromSeconds(30));
        builder.Services.AddScoped<IPaymentProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http    = factory.CreateClient(nameof(PayUPaymentProvider));
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PayUOptions>>();
            var logger  = sp.GetRequiredService<ILogger<PayUPaymentProvider>>();
            return new PayUPaymentProvider(http, options, logger);
        });

        // Weather: singleton that creates its HttpClient once via IHttpClientFactory,
        // which avoids the socket-exhaustion problem and works correctly with a singleton.
        builder.Services.AddHttpClient("WeatherClient", client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        builder.Services.AddSingleton<WeatherService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var http    = factory.CreateClient("WeatherClient");
            var cfg     = sp.GetRequiredService<IConfiguration>();
            var logger  = sp.GetRequiredService<ILogger<WeatherService>>();
            return new WeatherService(http, cfg, logger);
        });

        // ── App-layer singletons ──────────────────────────────────────────────
        builder.Services.AddSingleton<JwtTokenService>();

        // Expose AuthenticationService under both the concrete type and the interface
        // so pages can use @inject AuthenticationService Auth  (concrete, richer API)
        // and components can use @inject IAuthenticationService Auth  (interface, testable).
        builder.Services.AddSingleton<AuthenticationService>();
        builder.Services.AddSingleton<IAuthenticationService>(
            sp => sp.GetRequiredService<AuthenticationService>());

        builder.Services.AddSingleton<QrNfcBridgeService>();
        builder.Services.AddSingleton<ProfilePictureService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

        // ── Scoped state ──────────────────────────────────────────────────────
        builder.Services.AddScoped<BookingStateService>();
        builder.Services.AddScoped<ToastService>();

        var app = builder.Build();

        // Restore session if "Remember Me" was used
        try
        {
            var auth = app.Services.GetRequiredService<AuthenticationService>();
            _ = auth.TryRestoreSessionAsync();
        }
        catch { /* non-fatal */ }

        // ── EF Migrations ─────────────────────────────────────────────────────
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "startup.log");
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
            db.Database.Migrate();
            // Shop does not rent gloves/helmets; remove legacy seed rows if present.
            db.Equipment
                .Where(e => e.Category == Domain.Enums.EquipmentCategory.Gloves
                         || e.Category == Domain.Enums.EquipmentCategory.Helmet)
                .ExecuteDelete();
            File.WriteAllText(logPath, $"[{DateTime.Now}] Startup OK. DB={localDbPath}");
        }
        catch (Exception ex)
        {
            // Log and continue — migration failure on an existing DB is recoverable.
            // The app can still run against the current schema.
            File.WriteAllText(logPath, $"[{DateTime.Now}] Migration error:\n{ex}");
            app.Services.GetService<ILoggerFactory>()
               ?.CreateLogger("Startup")
               .LogError(ex, "Migration failed. DB={Path}", localDbPath);
        }

        // ── Test service resolution ────────────────────────────────────────────
        try
        {
            using var scope = app.Services.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
            var weather = scope.ServiceProvider.GetRequiredService<WeatherService>();
            File.AppendAllText(logPath, $"\n[{DateTime.Now}] Service resolution OK");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"\n[{DateTime.Now}] Service resolution error:\n{ex}");
        }

        return app;
    }
}
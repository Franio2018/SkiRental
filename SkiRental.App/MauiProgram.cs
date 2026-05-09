using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // ── Blazor WebView ────────────────────────────────────────────────
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        // ── Configuration (appsettings.json) ──────────────────────────────
        // appsettings.json is an embedded resource in the MAUI project.
        // Override with environment-specific file or user secrets in development.
        var config = builder.Configuration;

        // ── EF Core – SQLite (offline first) ─────────────────────────────
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "SkiRental.db");
        var connectionString = $"Data Source={dbPath}";

        builder.Services.AddDbContext<SkiRentalDbContext>(opts =>
            opts.UseSqlite(connectionString)
                .EnableSensitiveDataLogging(false),
            ServiceLifetime.Scoped);

        // ── JWT configuration (from appsettings.json) ──────────────────────
        builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        // ── PayU configuration (from appsettings.json) ────────────────────
        builder.Services.Configure<PayUOptions>(config.GetSection(PayUOptions.SectionName));

        // ── HTTP Clients ───────────────────────────────────────────────────
        builder.Services.AddHttpClient<PayUPaymentProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddHttpClient<WeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ── SkiRental domain services (repos, BookingService, Analytics…) ─
        builder.Services.AddSkiRentalServices(connectionString);

        // ── Payment provider ──────────────────────────────────────────────
        builder.Services.AddScoped<IPaymentProvider, PayUPaymentProvider>();

        // ── App-layer services ────────────────────────────────────────────
        // JwtTokenService: Singleton — stateless, safe to share across requests.
        builder.Services.AddSingleton<JwtTokenService>();

        // AuthenticationService: Singleton — holds current session state for the app lifetime.
        // Uses IServiceScopeFactory internally to resolve Scoped DbContext safely.
        builder.Services.AddSingleton<AuthenticationService>();

        builder.Services.AddSingleton<QrNfcBridgeService>();
        builder.Services.AddSingleton<WeatherService>();
        builder.Services.AddSingleton<ProfilePictureService>();

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

        var app = builder.Build();

        // ── Apply EF migrations on startup ────────────────────────────────
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILoggerFactory>()
                ?.CreateLogger("Startup");
            logger?.LogError(ex, "Failed to apply SQLite migrations on startup.");
        }

        return app;
    }
}

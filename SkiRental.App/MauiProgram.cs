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
        // appsettings.json must be EmbeddedResource in the .csproj (see fix).
        // Without this the stream is null, config is empty, and JwtTokenService
        // throws on startup because SecretKey is an empty string.
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SkiRental.App.appsettings.json");
        if (stream is not null)
            builder.Configuration.AddJsonStream(stream);

        var config = builder.Configuration;

        // ── Database ──────────────────────────────────────────────────────────
        // Copy database to app directory and use local copy
        var sourceDbPath = Path.GetFullPath(Path.Combine("..", "SkiRental.Logic", "SkiRental.db"));
        var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "SkiRental.db");
        
        // Always delete local copy to ensure fresh migration
        if (File.Exists(localDbPath))
        {
            File.Delete(localDbPath);
        }
        
        // Copy database if it doesn't exist locally
        if (File.Exists(sourceDbPath))
        {
            File.Copy(sourceDbPath, localDbPath);
        }
        
        var connectionString = $"Data Source={localDbPath}";
        
        // Debug: Log the database path
        Console.WriteLine($"Source DB: {sourceDbPath}");
        Console.WriteLine($"Local DB: {localDbPath}");
        Console.WriteLine($"Local DB exists: {File.Exists(localDbPath)}");

        // Single call — registers DbContext + all repositories + domain services.
        builder.Services.AddSkiRentalServices(connectionString);

        // ── JWT — with safe fallback so an unconfigured key never crashes app ─
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
        builder.Services.AddHttpClient<PayUPaymentProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));

        builder.Services.AddHttpClient<WeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // ── Payment provider ──────────────────────────────────────────────────
        builder.Services.AddScoped<IPaymentProvider, PayUPaymentProvider>();

        // ── App-layer singletons ──────────────────────────────────────────────
        builder.Services.AddSingleton<JwtTokenService>();

        // Register BOTH concrete and interface — same instance, different lookup keys.
        // Pages inject: @inject AuthenticationService Auth
        // QrScannerPanel injects: @inject IAuthenticationService Auth
        builder.Services.AddSingleton<AuthenticationService>();
        builder.Services.AddSingleton<IAuthenticationService>(
            sp => sp.GetRequiredService<AuthenticationService>());

        builder.Services.AddSingleton<QrNfcBridgeService>();
        builder.Services.AddSingleton<WeatherService>();
        builder.Services.AddSingleton<ProfilePictureService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

        var app = builder.Build();

        // ── EF Migrations — log crash to file (no window = no UI for errors) ──
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "startup.log");
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
            
            // Check if database exists and has tables
            var canConnect = db.Database.CanConnect();
            Console.WriteLine($"Can connect to DB: {canConnect}");
            
            if (canConnect)
            {
                var tables = db.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
                Console.WriteLine($"Existing tables: {string.Join(", ", tables)}");
            }
            
            db.Database.Migrate();
            File.WriteAllText(logPath, $"[{DateTime.Now}] OK. DB={localDbPath}");
        }
        catch (Exception ex)
        {
            File.WriteAllText(logPath, $"[{DateTime.Now}] CRASH:\n{ex}");
            app.Services.GetService<ILoggerFactory>()
               ?.CreateLogger("Startup")
               .LogError(ex, "Migration failed. DB={Path}", localDbPath);
        }

        return app;
    }
}
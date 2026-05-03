using Microsoft.EntityFrameworkCore;
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
            // ── ZXing QR/Barcode scanner ───────────────────────────────────
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",    "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf",   "OpenSansSemibold");
                fonts.AddFont("Montserrat-Bold.ttf",     "MontserratBold");
            });

        // ── Blazor WebView ────────────────────────────────────────────────
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        //builder.Logging.AddDebug();
#endif

        // ── EF Core – SQLite (offline first) ─────────────────────────────
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory, "SkiRental.db");

        builder.Services.AddDbContext<SkiRentalDbContext>(opts =>
            opts.UseSqlite($"Data Source={dbPath}")
                .EnableSensitiveDataLogging(false), // wyłącz w prod
            ServiceLifetime.Scoped);

        // ── PayU configuration ────────────────────────────────────────────
        builder.Services.Configure<PayUOptions>(opts =>
        {
            // W produkcji odczytaj z appsettings.json lub Azure Key Vault
            opts.PosId        = "YOUR_POS_ID";
            opts.SignatureKey  = "YOUR_SIGNATURE_KEY";
            opts.ClientId      = "YOUR_CLIENT_ID";
            opts.ClientSecret  = "YOUR_CLIENT_SECRET";
            opts.BaseUrl       = "https://secure.snd.payu.com"; // sandbox
            opts.NotifyUrl     = "https://YOUR_API/api/payu/notify";
        });

        // ── JWT configuration ───────────────────────────────────────────────
        builder.Services.Configure<JwtOptions>(opts =>
        {
            // In production, use a secure, randomly generated key (at least 256 bits)
            // Store this securely in environment variables, Azure Key Vault, or appsettings.json
            opts.SecretKey = "ThisIsAVerySecureSecretKeyForJWTTokenGeneration123!@#";
            opts.Issuer = "https://skirent.app";
            opts.Audience = "SkiRental.App";
            opts.ExpirationMinutes = 60; // 1 hour
            opts.ClockSkewMinutes = 5; // 5 minutes
        });

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

        // ── SkiRental Services (includes repositories and domain services) ────────
        builder.Services.AddSkiRentalServices($"Data Source={dbPath}");

        // ── Payment ────────────────────────────────────────────────────────
        builder.Services.AddScoped<IPaymentProvider, PayUPaymentProvider>();

        // ── JWT Token Service ─────────────────────────────────────────────────
        builder.Services.AddSingleton<JwtTokenService>();

        // ── MAUI-specific services ─────────────────────────────────────────
        builder.Services.AddSingleton<QrNfcBridgeService>();
        builder.Services.AddSingleton<WeatherService>();
        builder.Services.AddSingleton<ProfilePictureService>();
        builder.Services.AddSingleton<AuthenticationService>();
        
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<App>();

        var app = builder.Build();

        // Ensure the SQLite schema exists (migrations) before any pages query data
        // e.g. Analytics tab. In MAUI we can't easily await here, so run and log failures.
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("Startup");
            logger?.LogError(ex, "Failed to migrate SQLite database on startup.");
        }

        return app;
    }
}

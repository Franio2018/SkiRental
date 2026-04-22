using Microsoft.EntityFrameworkCore;
// NOTE: UseInMemoryDatabase() requires the NuGet package:
//   Microsoft.EntityFrameworkCore.InMemory v9.0.x
// The method lives in the Microsoft.EntityFrameworkCore namespace (no extra using needed).
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;

namespace SkiRental.Logic;

/// <summary>
/// Extension metody do rejestracji serwisów warstwy logiki.
/// Umożliwia współdzielenie konfiguracji DI między projektem MAUI
/// a ewentualnym projektem ASP.NET Core back-office.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje wszystkie serwisy domeny i logiki biznesowej.
    /// </summary>
    /// <param name="services">Kolekcja serwisów DI.</param>
    /// <param name="connectionString">Connection string do bazy danych.</param>
    /// <param name="useInMemory">Użyj bazy in-memory (do testów).</param>
    public static IServiceCollection AddSkiRentalServices(
        this IServiceCollection services,
        string? connectionString = null,
        bool useInMemory = false)
    {
        // ── EF Core DbContext ──────────────────────────────────────────────
        if (useInMemory)
        {
            services.AddDbContext<SkiRentalDbContext>(opts =>
                opts.UseInMemoryDatabase("SkiRentalTest"));
        }
        else if (connectionString is not null)
        {
            // Auto-detect: SQL Server vs SQLite
            services.AddDbContext<SkiRentalDbContext>(opts =>
            {
                if (connectionString.Contains("Data Source=") ||
                    connectionString.EndsWith(".db"))
                    opts.UseSqlite(connectionString);
                else
                    opts.UseSqlServer(connectionString);
            });
        }

        // ── Domain Services ────────────────────────────────────────────────
        services.AddScoped<EquipmentMathService>();
        services.AddScoped<BookingService>();
        services.AddScoped<AnalyticsService>();

        return services;
    }

    /// <summary>
    /// Stosuje migracje bazy danych przy starcie aplikacji.
    /// Wywoływane z MauiProgram lub Program.cs web.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
        await db.Database.MigrateAsync();
    }
}

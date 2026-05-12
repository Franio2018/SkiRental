using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;

namespace SkiRental.Logic;

/// <summary>
/// Extension methods for registering all SkiRental domain and logic services.
/// Shared between the MAUI app and any future ASP.NET Core back-office.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DbContext, repositories, and domain services.
    /// </summary>
    /// <param name="services">DI service collection.</param>
    /// <param name="connectionString">Database connection string. Pass null only when useInMemory is true.</param>
    /// <param name="useInMemory">Use in-memory database (unit tests only).</param>
    public static IServiceCollection AddSkiRentalServices(
        this IServiceCollection services,
        string? connectionString = null,
        bool useInMemory = false)
    {
        // ── EF Core DbContext ──────────────────────────────────────────────────
        if (useInMemory)
        {
            services.AddDbContext<SkiRentalDbContext>(opts =>
                opts.UseInMemoryDatabase("SkiRentalTest"));
        }
        else if (connectionString is not null)
        {
            services.AddDbContext<SkiRentalDbContext>(opts =>
            {
                if (connectionString.Contains("Data Source=") ||
                    connectionString.EndsWith(".db"))
                    opts.UseSqlite(connectionString);
                else
                    opts.UseSqlServer(connectionString);
            });
        }

        // ── Repositories ───────────────────────────────────────────────────────
        services.AddScoped<IUserRepository,      EfUserRepository>();
        services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        services.AddScoped<IRentalRepository,    EfRentalRepository>();

        // ── Domain services ────────────────────────────────────────────────────
        services.AddScoped<EquipmentMathService>();
        services.AddScoped<BookingService>();
        services.AddScoped<AnalyticsService>();
        services.AddScoped<UserService>();
        
        // ── New rental flow services ───────────────────────────────────────────
        services.AddScoped<IRecommendationOrchestrator, RecommendationOrchestrator>();
        services.AddScoped<IGearSwapService, GearSwapService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    /// <summary>
    /// Applies EF Core migrations. Call once at app startup (MAUI or web host).
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
        await db.Database.MigrateAsync();
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Options;
using SkiRental.Logic.Services;

namespace SkiRental.Tests.Infrastructure;

/// <summary>
/// Shared in-memory SQLite database for integration tests (single connection, Cache=Shared).
/// </summary>
public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _dbId = Guid.NewGuid().ToString("N");

    public IServiceProvider Services { get; private set; } = null!;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection($"Data Source={_dbId};Mode=Memory;Cache=Shared");
    }

    public async Task InitializeAsync(Action<IServiceCollection>? configure = null)
    {
        await _connection.OpenAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SkiRentalOptions.SectionName}:SeasonStart"] = "2024-12-01",
                [$"{SkiRentalOptions.SectionName}:SeasonEnd"] = "2025-03-31",
                [$"{SkiRentalOptions.SectionName}:MaxRentalDaysPerBooking"] = "14",
                [$"{SkiRentalOptions.SectionName}:LateFeePercentPerHour"] = "10",
                [$"{SkiRentalOptions.SectionName}:DamageSurchargeThresholdCondition"] = "3",
                [$"{SkiRentalOptions.SectionName}:DamageSurchargePercent"] = "50",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<SkiRentalDbContext>(opts =>
            opts.UseSqlite(_connection));

        services.AddSkiRentalServices(configuration: config);
        services.AddScoped<IPaymentProvider, MockPaymentProvider>();

        configure?.Invoke(services);

        Services = services.BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task<(UserProfile Customer, Equipment Skis, Equipment Poles, Equipment Boots)> SeedRentalScenarioAsync(
        CancellationToken ct = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRentalDbContext>();

        var customer = new UserProfile
        {
            FirstName = "Test",
            LastName = "Customer",
            Height = 175,
            Weight = 70m,
            ShoeSize = 42,
            SkillLevel = SkillLevel.Intermediate,
            PreferredStyle = SkiStyle.Classic,
            CreatedAt = DateTime.UtcNow
        };
        db.UserProfiles.Add(customer);

        var skis = new Equipment
        {
            SerialNumber = $"TEST-SKI-{_dbId[..8]}",
            Brand = "Test",
            Model = "Classic",
            Category = EquipmentCategory.Skis,
            Size = 197.5m,
            Stiffness = 52,
            Status = EquipmentStatus.Available,
            CompatibleStyle = SkiStyle.Classic,
            YearOfProduction = 2024,
            DailyRate = 80m
        };
        var poles = new Equipment
        {
            SerialNumber = $"TEST-POLE-{_dbId[..8]}",
            Brand = "Test",
            Model = "Pole",
            Category = EquipmentCategory.Poles,
            Size = 147.5m,
            Status = EquipmentStatus.Available,
            CompatibleStyle = SkiStyle.Classic,
            YearOfProduction = 2024,
            DailyRate = 15m
        };
        var boots = new Equipment
        {
            SerialNumber = $"TEST-BOOT-{_dbId[..8]}",
            Brand = "Test",
            Model = "Boot",
            Category = EquipmentCategory.Boots,
            Size = 42m,
            Status = EquipmentStatus.Available,
            YearOfProduction = 2024,
            DailyRate = 25m
        };

        db.Equipment.AddRange(skis, poles, boots);
        await db.SaveChangesAsync(ct);

        return (customer, skis, poles, boots);
    }

    public async ValueTask DisposeAsync()
    {
        if (Services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (Services is IDisposable disposable)
            disposable.Dispose();

        await _connection.DisposeAsync();
    }
}

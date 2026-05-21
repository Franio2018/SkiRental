using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using SkiRental.Tests.Infrastructure;
using Xunit;

namespace SkiRental.Tests;

public class EfRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly SqliteTestDatabase _db = new();

    public async Task InitializeAsync() => await _db.InitializeAsync();

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task UserRepository_CreateAndGetByUsername_IsCaseInsensitive()
    {
        await using var scope = _db.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        await repo.CreateAsync(new AppUser
        {
            Username = "TestUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Role = AppRole.Customer.ToString(),
        });

        var found = await repo.GetByUsernameAsync("TESTUSER");
        found.Should().NotBeNull();
        found!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task EquipmentRepository_UpdateStatus_Persists()
    {
        await using var scope = _db.Services.CreateAsyncScope();
        var equipRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();

        var eq = await equipRepo.CreateAsync(new Equipment
        {
            SerialNumber = "INT-EQ-1",
            Brand = "T",
            Model = "M",
            Category = EquipmentCategory.Skis,
            Size = 190m,
            Status = EquipmentStatus.Available,
            YearOfProduction = 2024,
            DailyRate = 40m,
        });

        await equipRepo.UpdateStatusAsync(eq.Id, EquipmentStatus.Reserved);

        await using var scope2 = _db.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();
        (await db2.Equipment.FindAsync(eq.Id))!.Status.Should().Be(EquipmentStatus.Reserved);
    }

    [Fact]
    public async Task RentalRepository_GetByQrCode_ReturnsRentalWithEquipment()
    {
        await using var scope = _db.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();
        var rentalRepo = scope.ServiceProvider.GetRequiredService<IRentalRepository>();

        var customer = new UserProfile
        {
            FirstName = "Qr",
            LastName = "Test",
            Height = 180,
            Weight = 75m,
            ShoeSize = 43,
            SkillLevel = SkillLevel.Beginner,
            PreferredStyle = SkiStyle.Classic,
            CreatedAt = DateTime.UtcNow,
        };
        db.UserProfiles.Add(customer);

        var skis = new Equipment
        {
            SerialNumber = "QR-SKI",
            Brand = "T",
            Model = "S",
            Category = EquipmentCategory.Skis,
            Size = 190m,
            Status = EquipmentStatus.Reserved,
            YearOfProduction = 2024,
            DailyRate = 50m,
        };
        db.Equipment.Add(skis);
        await db.SaveChangesAsync();

        var rental = new Rental
        {
            CustomerProfileId = customer.Id,
            StartTime = new DateTime(2025, 1, 15, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 1, 16, 9, 0, 0, DateTimeKind.Utc),
            QrCode = "SKI-QR-TEST-001",
            Status = RentalStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            TotalAmount = 100m,
            RentalEquipments =
            [
                new RentalEquipment { EquipmentId = skis.Id, ConditionAtPickup = 5 },
            ],
        };

        await rentalRepo.CreateAsync(rental);

        var found = await rentalRepo.GetByQrCodeAsync("SKI-QR-TEST-001");
        found.Should().NotBeNull();
        found!.RentalEquipments.Should().HaveCount(1);
        found.RentalEquipments.First().Equipment!.SerialNumber.Should().Be("QR-SKI");
    }
}

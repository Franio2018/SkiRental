using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using SkiRental.Tests.Infrastructure;
using Xunit;

namespace SkiRental.Tests;

public class BookingServiceTests : IAsyncLifetime
{
    private readonly SqliteTestDatabase _db = new();
    private UserProfile _customer = null!;
    private Equipment _skis = null!;
    private Equipment _poles = null!;
    private Equipment _boots = null!;

    private static readonly DateTime SeasonStart = new(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SeasonEnd = SeasonStart.AddDays(2);

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        (_customer, _skis, _poles, _boots) = await _db.SeedRentalScenarioAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<BookingService> CreateBookingServiceAsync()
    {
        var scope = _db.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<BookingService>();
    }

    private CreateBookingRequest DefaultRequest(params int[] equipmentIds) =>
        new()
        {
            CustomerProfileId = _customer.Id,
            EquipmentIds = equipmentIds,
            StartTime = SeasonStart,
            EndTime = SeasonEnd,
        };

    [Fact]
    public async Task CreateBookingAsync_ReservesEquipment_AndSetsPending()
    {
        var booking = await CreateBookingServiceAsync();
        var result = await booking.CreateBookingAsync(DefaultRequest(_skis.Id, _poles.Id, _boots.Id));

        result.IsSuccess.Should().BeTrue();
        result.QrCode.Should().NotBeNullOrEmpty();

        await using var scope = _db.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();

        var rental = await db.Rentals
            .Include(r => r.RentalEquipments)
            .FirstAsync(r => r.Id == result.RentalId!.Value);
        rental.Status.Should().Be(RentalStatus.Pending);
        rental.RentalEquipments.Should().HaveCount(3);

        (await db.Equipment.FindAsync(_skis.Id))!.Status.Should().Be(EquipmentStatus.Reserved);
        (await db.Equipment.FindAsync(_poles.Id))!.Status.Should().Be(EquipmentStatus.Reserved);
        (await db.Equipment.FindAsync(_boots.Id))!.Status.Should().Be(EquipmentStatus.Reserved);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenEquipmentAlreadyReserved_Fails()
    {
        var booking = await CreateBookingServiceAsync();
        var first = await booking.CreateBookingAsync(DefaultRequest(_skis.Id));
        first.IsSuccess.Should().BeTrue();

        var second = await booking.CreateBookingAsync(DefaultRequest(_skis.Id));
        second.IsSuccess.Should().BeFalse();
        second.ErrorMessage.Should().Contain("niedostępny");
    }

    [Fact]
    public async Task CreateBookingAsync_OutsideSeason_Fails()
    {
        var booking = await CreateBookingServiceAsync();
        var result = await booking.CreateBookingAsync(new CreateBookingRequest
        {
            CustomerProfileId = _customer.Id,
            EquipmentIds = [_skis.Id],
            StartTime = new DateTime(2026, 6, 1),
            EndTime = new DateTime(2026, 6, 3),
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("sezonie");
    }

    [Fact]
    public async Task ActivateRentalAsync_MovesEquipmentToRented()
    {
        var booking = await CreateBookingServiceAsync();
        var created = await booking.CreateBookingAsync(DefaultRequest(_skis.Id));
        created.IsSuccess.Should().BeTrue();

        var activation = await booking.ActivateRentalAsync(created.RentalId!.Value, workerId: 2);
        activation.IsSuccess.Should().BeTrue();

        await using var scope = _db.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();

        (await db.Rentals.FindAsync(created.RentalId!.Value))!.Status.Should().Be(RentalStatus.Active);
        (await db.Equipment.FindAsync(_skis.Id))!.Status.Should().Be(EquipmentStatus.Rented);
    }

    [Fact]
    public async Task ProcessReturnByQrCodeAsync_CompletesRental_AndFreesEquipment()
    {
        var booking = await CreateBookingServiceAsync();
        var created = await booking.CreateBookingAsync(DefaultRequest(_skis.Id));
        await booking.ActivateRentalAsync(created.RentalId!.Value, workerId: 2);

        var rental = await booking.GetRentalByQrCodeAsync(created.QrCode!);
        rental.Should().NotBeNull();

        var returned = await booking.ProcessReturnByQrCodeAsync(
            created.QrCode!,
            new Dictionary<int, int> { [_skis.Id] = 4 });

        returned.IsSuccess.Should().BeTrue();
        returned.RentalId.Should().Be(created.RentalId);

        await using var scope = _db.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();

        (await db.Rentals.FindAsync(created.RentalId!.Value))!.Status.Should().Be(RentalStatus.Completed);
        (await db.Equipment.FindAsync(_skis.Id))!.Status.Should().Be(EquipmentStatus.Available);
    }

    [Fact]
    public async Task CancelBookingAsync_ReleasesReservedEquipment()
    {
        var booking = await CreateBookingServiceAsync();
        var created = await booking.CreateBookingAsync(DefaultRequest(_skis.Id, _poles.Id));
        created.IsSuccess.Should().BeTrue();

        var cancelled = await booking.CancelBookingAsync(created.RentalId!.Value);
        cancelled.IsSuccess.Should().BeTrue();

        await using var scope = _db.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();

        (await db.Rentals.FindAsync(created.RentalId!.Value))!.Status.Should().Be(RentalStatus.Cancelled);
        (await db.Equipment.FindAsync(_skis.Id))!.Status.Should().Be(EquipmentStatus.Available);
        (await db.Equipment.FindAsync(_poles.Id))!.Status.Should().Be(EquipmentStatus.Available);
    }

    [Fact]
    public async Task CancelBookingAsync_ActiveRental_IsRejected()
    {
        var booking = await CreateBookingServiceAsync();
        var created = await booking.CreateBookingAsync(DefaultRequest(_skis.Id));
        await booking.ActivateRentalAsync(created.RentalId!.Value, workerId: 2);

        var cancelled = await booking.CancelBookingAsync(created.RentalId!.Value);
        cancelled.IsSuccess.Should().BeFalse();
        cancelled.ErrorMessage.Should().Contain("aktywnego");
    }

    [Fact]
    public async Task CreateBookingAsync_BlikFailure_RollsBackReservation()
    {
        await using var localDb = new SqliteTestDatabase();
        await localDb.InitializeAsync(services =>
            services.AddScoped<IPaymentProvider>(_ => new MockPaymentProvider { BlikSucceeds = false }));

        var (customer, skis, _, _) = await localDb.SeedRentalScenarioAsync();

        await using var scope = localDb.Services.CreateAsyncScope();
        var booking = scope.ServiceProvider.GetRequiredService<BookingService>();

        var result = await booking.CreateBookingAsync(new CreateBookingRequest
        {
            CustomerProfileId = customer.Id,
            EquipmentIds = [skis.Id],
            StartTime = SeasonStart,
            EndTime = SeasonEnd,
            BlikCode = "123456",
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BLIK");

        await using var verifyScope = localDb.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SkiRental.Logic.SkiRentalDbContext>();
        (await verifyDb.Equipment.FindAsync(skis.Id))!.Status.Should().Be(EquipmentStatus.Available);
    }
}

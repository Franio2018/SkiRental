using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using SkiRental.Logic.Options;
using SkiRental.Logic.Services;

namespace SkiRental.Tests;

public class RentalRulesServiceTests
{
    private static RentalRulesService CreateService() =>
        new(Options.Create(new SkiRentalOptions
        {
            SeasonStart = new DateTime(2024, 12, 1),
            SeasonEnd = new DateTime(2025, 3, 31),
            MaxRentalDaysPerBooking = 14,
            LateFeePercentPerHour = 10,
            DamageSurchargeThresholdCondition = 3,
            DamageSurchargePercent = 50
        }));

    [Fact]
    public void ValidateBookingWindow_RejectsTooLong()
    {
        var svc = CreateService();
        var start = new DateTime(2025, 1, 1);
        var end = start.AddDays(20);
        var (ok, err) = svc.ValidateBookingWindow(start, end);
        ok.Should().BeFalse();
        err.Should().Contain("14");
    }

    [Fact]
    public void CalculateLateFee_AppliesWhenOverdue()
    {
        var svc = CreateService();
        var fee = svc.CalculateLateFee(
            new DateTime(2025, 1, 10, 12, 0, 0),
            new DateTime(2025, 1, 10, 14, 0, 0),
            100m);
        fee.Should().Be(20m); // 2h * 10% * 100
    }

    [Fact]
    public void CalculateDamageSurcharge_WhenConditionPoor()
    {
        var svc = CreateService();
        var fee = svc.CalculateDamageSurcharge(200m, 2);
        fee.Should().Be(100m);
    }
}

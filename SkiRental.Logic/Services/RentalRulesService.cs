using Microsoft.Extensions.Options;
using SkiRental.Logic.Options;

namespace SkiRental.Logic.Services;

/// <summary>Validates rental windows and calculates late/damage fees from <see cref="SkiRentalOptions"/>.</summary>
public sealed class RentalRulesService
{
    private readonly SkiRentalOptions _opts;

    public RentalRulesService(IOptions<SkiRentalOptions> options)
        => _opts = options.Value;

    public (bool IsValid, string? Error) ValidateBookingWindow(DateTime start, DateTime end)
    {
        if (end <= start)
            return (false, "Data zakończenia musi być późniejsza niż rozpoczęcia.");

        var days = (end - start).TotalDays;
        if (days > _opts.MaxRentalDaysPerBooking)
            return (false, $"Maksymalny czas wypożyczenia to {_opts.MaxRentalDaysPerBooking} dni.");

        if (start.Date < _opts.SeasonStart.Date || end.Date > _opts.SeasonEnd.Date)
            return (false, $"Wypożyczenia dostępne w sezonie {_opts.SeasonStart:dd.MM.yyyy} – {_opts.SeasonEnd:dd.MM.yyyy}.");

        return (true, null);
    }

    public decimal CalculateLateFee(DateTime plannedEnd, DateTime actualReturn, decimal rentalTotal)
    {
        if (actualReturn <= plannedEnd) return 0m;
        var lateHours = (decimal)(actualReturn - plannedEnd).TotalHours;
        return Math.Round(rentalTotal * (decimal)(_opts.LateFeePercentPerHour / 100.0) * lateHours, 2);
    }

    public decimal CalculateDamageSurcharge(decimal rentalTotal, int worstCondition)
    {
        if (worstCondition >= _opts.DamageSurchargeThresholdCondition) return 0m;
        return Math.Round(rentalTotal * (decimal)(_opts.DamageSurchargePercent / 100.0), 2);
    }
}

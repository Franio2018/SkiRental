using SkiRental.Logic.Options;

namespace SkiRental.Logic.Services;

/// <summary>Domyślne i normalizowane okna dat wypożyczenia (tylko kalendarz, bez godziny w UI).</summary>
public static class RentalWindowHelper
{
    public const int DefaultSpanDays = 7;

    /// <summary>Data rozpoczęcia i zakończenia (same daty, bez godzin).</summary>
    public static (DateTime StartDate, DateTime EndDate) GetDefaultDates(SkiRentalOptions opts)
    {
        var seasonStart = opts.SeasonStart.Date;
        var seasonEnd = opts.SeasonEnd.Date;
        var today = DateTime.Today;

        // Poza sezonem: pierwszy tydzień sezonu zamiast „dzisiaj”.
        var start = today < seasonStart || today > seasonEnd
            ? seasonStart
            : today;

        var end = start.AddDays(DefaultSpanDays);
        if (end > seasonEnd)
            end = seasonEnd;

        var maxSpan = Math.Max(1, opts.MaxRentalDaysPerBooking);
        if ((end - start).Days > maxSpan)
            end = start.AddDays(maxSpan);

        if (end <= start)
            end = start.AddDays(Math.Min(1, Math.Max(0, (seasonEnd - start).Days)));

        return (start, end);
    }

    /// <summary>Godziny odbioru/zwrotu używane w bazie i regułach biznesowych.</summary>
    public static (DateTime Start, DateTime End) ToBookingWindow(DateTime startDate, DateTime endDate)
        => (startDate.Date.AddHours(9), endDate.Date.AddHours(18));

    public static int CountRentalDays(DateTime start, DateTime end)
        => Math.Max(1, (end.Date - start.Date).Days);
}

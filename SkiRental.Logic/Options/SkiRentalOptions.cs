namespace SkiRental.Logic.Options;

/// <summary>Business rules from appsettings.json section "SkiRental".</summary>
public sealed class SkiRentalOptions
{
    public const string SectionName = "SkiRental";

    public DateTime SeasonStart { get; set; } = new(2024, 12, 1);
    public DateTime SeasonEnd { get; set; } = new(2025, 3, 31);
    public int MaxRentalDaysPerBooking { get; set; } = 14;
    public double LateFeePercentPerHour { get; set; } = 10.0;
    public int DamageSurchargeThresholdCondition { get; set; } = 3;
    public double DamageSurchargePercent { get; set; } = 50.0;
    public string SupportEmail { get; set; } = "kontakt@skirent.pl";
    public string SupportPhone { get; set; } = "+48 18 000 0000";
}

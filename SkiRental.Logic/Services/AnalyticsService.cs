using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Serwis analityczny dla back-office (Owner / Worker).
/// Dostarcza wskaźniki KPI dla zarządzania flotą sprzętu i przychodami.
/// </summary>
public sealed class AnalyticsService
{
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IRentalRepository    _rentalRepo;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IEquipmentRepository equipmentRepo,
        IRentalRepository rentalRepo,
        ILogger<AnalyticsService> logger)
    {
        _equipmentRepo = equipmentRepo;
        _rentalRepo    = rentalRepo;
        _logger        = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Utilization rate – core KPI
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Oblicza wskaźnik użycia sprzętu (UtilizationRate) dla danego sezonu.
    /// </summary>
    public async Task<UtilizationReport> CalculateUtilizationRateAsync(
        int equipmentId,
        DateTime seasonStart,
        DateTime seasonEnd,
        CancellationToken ct = default)
    {
        var equipment = await _equipmentRepo.GetByIdAsync(equipmentId, ct)
            ?? throw new ArgumentException($"Sprzęt o ID {equipmentId} nie istnieje.");

        var rentals = await _rentalRepo.GetRentalsForUtilizationAsync(
            equipmentId, seasonStart, seasonEnd, ct);

        double totalRentedHours = rentals
            .Where(r => r.Status is RentalStatus.Active or RentalStatus.Completed)
            .Sum(r =>
            {
                var endTime = r.ActualReturnTime ?? r.EndTime;
                var start   = r.StartTime < seasonStart ? seasonStart : r.StartTime;
                var end     = endTime > seasonEnd ? seasonEnd : endTime;
                return end > start ? (end - start).TotalHours : 0;
            });

        double seasonTotalHours = (seasonEnd - seasonStart).TotalHours;

        decimal revenue = rentals
            .Where(r => r.PaymentStatus == PaymentStatus.Paid)
            .Sum(r => r.TotalAmount);

        _logger.LogDebug(
            "Utilization Equipment {Id}: {Hours}h / {Total}h = {Rate:P1}",
            equipmentId, totalRentedHours, seasonTotalHours,
            seasonTotalHours > 0 ? totalRentedHours / seasonTotalHours : 0);

        return new UtilizationReport
        {
            EquipmentId          = equipmentId,
            SerialNumber         = equipment.SerialNumber,
            EquipmentDescription = $"{equipment.Brand} {equipment.Model} ({equipment.Size} cm)",
            Category             = equipment.Category,
            TotalRentedHours     = Math.Round(totalRentedHours, 2),
            SeasonAvailableHours = Math.Round(seasonTotalHours, 2),
            TotalRentals         = rentals.Count,
            Revenue              = revenue,
            SeasonStart          = seasonStart,
            SeasonEnd            = seasonEnd
        };
    }

    /// <summary>
    /// Raport użycia dla całej floty – posortowany malejąco wg UtilizationRate.
    /// </summary>
    public async Task<IReadOnlyList<UtilizationReport>> GetFleetUtilizationReportAsync(
        DateTime seasonStart,
        DateTime seasonEnd,
        CancellationToken ct = default)
    {
        var allEquipment = await _equipmentRepo.GetAllAsync(ct);

        var tasks = allEquipment
            .Select(e => CalculateUtilizationRateAsync(e.Id, seasonStart, seasonEnd, ct));

        var reports = await Task.WhenAll(tasks);

        return reports
            .OrderByDescending(r => r.UtilizationRate)
            .ToList()
            .AsReadOnly();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Revenue report
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raport przychodów dzienny/tygodniowy/miesięczny dla właściciela.
    /// </summary>
    public async Task<RevenueReport> GetRevenueReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rentals = await _rentalRepo.GetAllRentalsAsync(from, to, ct);

        var paid = rentals.Where(r => r.PaymentStatus == PaymentStatus.Paid).ToList();

        var byCategory = paid
            .SelectMany(r => r.RentalEquipments)
            .GroupBy(re => re.Equipment.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(re =>
                {
                    var rental = paid.First(r => r.RentalEquipments.Contains(re));
                    // Proportional revenue split by item count
                    return rental.TotalAmount / rental.RentalEquipments.Count;
                })
            );

        return new RevenueReport
        {
            From               = from,
            To                 = to,
            TotalRevenue       = paid.Sum(r => r.TotalAmount),
            TotalRentals       = rentals.Count,
            PaidRentals        = paid.Count,
            CancelledRentals   = rentals.Count(r => r.Status == RentalStatus.Cancelled),
            OverdueRentals     = rentals.Count(r => r.Status == RentalStatus.Overdue),
            RevenueByCategory  = byCategory,
            AverageRentalValue = paid.Count > 0 ? paid.Average(r => r.TotalAmount) : 0
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Average rental duration (BUG FIX)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Oblicza średni czas trwania wypożyczenia [h] w podanym przedziale dat.
    ///
    /// BUG FIX vs original: original called GetActiveRentalsAsync() (Pending+Active only)
    /// then filtered for Completed — always returned 0. This now uses GetAllRentalsAsync.
    /// </summary>
    public async Task<double> GetAverageRentalDurationAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rentals = await _rentalRepo.GetAllRentalsAsync(from, to, ct);

        var completed = rentals
            .Where(r => r.Status == RentalStatus.Completed)
            .ToList();

        if (completed.Count == 0) return 0;

        return completed.Average(r =>
            ((r.ActualReturnTime ?? r.EndTime) - r.StartTime).TotalHours);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Maintenance alerts
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identyfikuje sprzęt wymagający przeglądu:
    ///   – nie serwisowany ponad 30 dni, lub
    ///   – nigdy nie serwisowany.
    /// </summary>
    public async Task<IReadOnlyList<Equipment>> GetEquipmentRequiringMaintenanceAsync(
        CancellationToken ct = default)
    {
        var all = await _equipmentRepo.GetAllAsync(ct);
        var threshold = DateTime.UtcNow.AddDays(-30);

        return all
            .Where(e => e.Status != EquipmentStatus.Retired &&
                        (e.LastMaintenanceDate is null || e.LastMaintenanceDate < threshold))
            .ToList()
            .AsReadOnly();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Report DTOs
// ═══════════════════════════════════════════════════════════════════════════════


public record RevenueReport
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalRentals { get; init; }
    public int PaidRentals { get; init; }
    public int CancelledRentals { get; init; }
    public int OverdueRentals { get; init; }
    public decimal AverageRentalValue { get; init; }
    public Dictionary<EquipmentCategory, decimal> RevenueByCategory { get; init; } = new();
}

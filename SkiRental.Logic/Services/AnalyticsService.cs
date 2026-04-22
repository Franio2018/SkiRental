using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Serwis analityczny dla back-office.
/// Dostarcza wskaźniki KPI dla zarządzania flotą sprzętu.
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
    // UtilizationRate – kluczowy KPI
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Oblicza wskaźnik użycia sprzętu dla danego sezonu:
    ///   UtilizationRate = Σ(czas wypożyczeń) / Całkowity dostępny czas sezonu
    ///
    /// Całkowity dostępny czas = (SeasonEnd – SeasonStart) w godzinach.
    /// Czas serwisowy jest odejmowany od mianownika.
    /// </summary>
    /// <param name="equipmentId">ID sprzętu do analizy.</param>
    /// <param name="seasonStart">Początek sezonu.</param>
    /// <param name="seasonEnd">Koniec sezonu.</param>
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

        // Zsumuj czas aktywnych wypożyczeń (godziny)
        double totalRentedHours = rentals
            .Where(r => r.Status is RentalStatus.Active or RentalStatus.Completed)
            .Sum(r =>
            {
                var endTime = r.ActualReturnTime ?? r.EndTime;
                // Ogranicz do granic sezonu
                var start = r.StartTime < seasonStart ? seasonStart : r.StartTime;
                var end   = endTime > seasonEnd ? seasonEnd : endTime;
                return end > start ? (end - start).TotalHours : 0;
            });

        // Całkowity dostępny czas sezonu [h]
        double seasonTotalHours = (seasonEnd - seasonStart).TotalHours;

        // Przychód
        decimal revenue = rentals
            .Where(r => r.PaymentStatus == PaymentStatus.Paid)
            .Sum(r => r.TotalAmount);

        _logger.LogDebug(
            "Utilization for Equipment {Id}: {Hours}h rented / {Total}h available = {Rate:P1}",
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
    // Dodatkowe KPI
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Oblicza średni czas trwania wypożyczenia [h].</summary>
    public async Task<double> GetAverageRentalDurationAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rentals = await _rentalRepo.GetActiveRentalsAsync(ct);
        var filtered = rentals
            .Where(r => r.StartTime >= from && r.StartTime <= to && r.Status == RentalStatus.Completed)
            .ToList();

        if (!filtered.Any()) return 0;

        return filtered.Average(r =>
            ((r.ActualReturnTime ?? r.EndTime) - r.StartTime).TotalHours);
    }

    /// <summary>
    /// Identyfikuje sprzęt wymagający przeglądu
    /// (utilization > 80% lub nie serwisowany > 30 dni).
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

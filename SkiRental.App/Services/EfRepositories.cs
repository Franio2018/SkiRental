using Microsoft.EntityFrameworkCore;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic;
using SkiRental.Logic.Interfaces;

namespace SkiRental.App.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// EfEquipmentRepository
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class EfEquipmentRepository : IEquipmentRepository
{
    private readonly SkiRentalDbContext _db;

    public EfEquipmentRepository(SkiRentalDbContext db) => _db = db;

    public async Task<IReadOnlyList<Equipment>> GetAvailableByStyleAsync(
        SkiStyle style, EquipmentCategory category, CancellationToken ct = default)
    {
        return await _db.Equipment
            .Where(e => e.Status == EquipmentStatus.Available
                     && e.Category == category
                     && (e.CompatibleStyle == style || e.CompatibleStyle == SkiStyle.Combi))
            .OrderBy(e => e.Size)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Equipment.FindAsync([id], ct);

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default)
        => await _db.Equipment.AsNoTracking().ToListAsync(ct);

    public async Task UpdateStatusAsync(int equipmentId, EquipmentStatus status,
        CancellationToken ct = default)
    {
        await _db.Equipment
            .Where(e => e.Id == equipmentId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, status), ct);
    }

    public async Task<IReadOnlyList<Equipment>> GetByRentalIdAsync(int rentalId,
        CancellationToken ct = default)
    {
        return await _db.RentalEquipments
            .Where(re => re.RentalId == rentalId)
            .Select(re => re.Equipment)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Equipment> CreateAsync(Equipment equipment, CancellationToken ct = default)
    {
        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync(ct);
        return equipment;
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken ct = default)
    {
        _db.Equipment.Update(equipment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => await _db.Equipment.AnyAsync(e => e.Id == id, ct);
}

// ═══════════════════════════════════════════════════════════════════════════════
// EfRentalRepository
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class EfRentalRepository : IRentalRepository
{
    private readonly SkiRentalDbContext _db;

    public EfRentalRepository(SkiRentalDbContext db) => _db = db;

    public async Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Rentals
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Rental?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default)
        => await _db.Rentals
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .FirstOrDefaultAsync(r => r.QrCode == qrCode, ct);

    public async Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default)
        => await _db.Rentals
            .Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Pending)
            .Include(r => r.Customer)
            .OrderBy(r => r.EndTime)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rental>> GetAllRentalsAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Rentals
            .Where(r => r.StartTime >= from && r.StartTime <= to)
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderByDescending(r => r.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId,
        CancellationToken ct = default)
        => await _db.Rentals
            .Where(r => r.CustomerProfileId == customerId)
            .Include(r => r.RentalEquipments)
            .OrderByDescending(r => r.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default)
    {
        _db.Rentals.Add(rental);
        await _db.SaveChangesAsync(ct);
        return rental;
    }

    public async Task UpdateAsync(Rental rental, CancellationToken ct = default)
    {
        _db.Rentals.Update(rental);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Rental>> GetRentalsForUtilizationAsync(
        int equipmentId, DateTime seasonStart, DateTime seasonEnd, CancellationToken ct = default)
    {
        return await _db.RentalEquipments
            .Where(re => re.EquipmentId == equipmentId
                      && re.Rental.StartTime < seasonEnd
                      && (re.Rental.ActualReturnTime ?? re.Rental.EndTime) > seasonStart)
            .Select(re => re.Rental)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, CustomerRentalSummary>>
        GetRentalSummariesByCustomerIdsAsync(IEnumerable<int> customerIds, CancellationToken ct = default)
    {
        var ids = customerIds.ToList();
        return await _db.Rentals
            .Where(r => ids.Contains(r.CustomerProfileId))
            .GroupBy(r => r.CustomerProfileId)
            .Select(g => new { CustomerId = g.Key, TotalRentals = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.CustomerId, x => new CustomerRentalSummary { TotalRentals = x.TotalRentals }, ct);
    }
}

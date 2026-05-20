using Microsoft.EntityFrameworkCore;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// EfUserRepository
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// Handles both <see cref="AppUser"/> (credentials) and the linked
/// <see cref="UserProfile"/> (physical measurements / preferences).
/// </summary>
public sealed class EfUserRepository : IUserRepository
{
    private readonly SkiRentalDbContext _db;

    public EfUserRepository(SkiRentalDbContext db) => _db = db;

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _db.AppUsers
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.AppUsers
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default)
        => await _db.AppUsers
            .Include(u => u.Profile)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<AppUser> CreateAsync(AppUser user, CancellationToken ct = default)
    {
        user.Username = user.Username.ToLowerInvariant();
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(AppUser user, CancellationToken ct = default)
    {
        user.Username = user.Username.ToLowerInvariant();
        _db.AppUsers.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _db.AppUsers.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => await _db.AppUsers
            .AnyAsync(u => u.Username == username.ToLowerInvariant(), ct);

    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<UserProfile?> GetProfileByUserIdAsync(int appUserId, CancellationToken ct = default)
    {
        var user = await _db.AppUsers
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == appUserId, ct);
        return user?.Profile;
    }

    /// <summary>
    /// Creates a new UserProfile (or updates an existing one) and links it
    /// to the given AppUser. Uses a single SaveChanges call.
    /// </summary>
    public async Task<UserProfile> UpsertProfileAsync(
        int appUserId, UserProfile profile, CancellationToken ct = default)
    {
        var user = await _db.AppUsers
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == appUserId, ct)
            ?? throw new InvalidOperationException($"AppUser {appUserId} not found.");

        if (user.Profile is null)
        {
            // Create new profile and link
            profile.CreatedAt = DateTime.UtcNow;
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);          // get profile.Id

            user.UserProfileId = profile.Id;
            _db.AppUsers.Update(user);
        }
        else
        {
            // Update existing profile fields (preserve Id and CreatedAt)
            var existing = user.Profile;
            existing.FirstName               = profile.FirstName;
            existing.LastName                = profile.LastName;
            existing.Email                   = profile.Email;
            existing.PhoneNumber             = profile.PhoneNumber;
            existing.Height                  = profile.Height;
            existing.Weight                  = profile.Weight;
            existing.ShoeSize                = profile.ShoeSize;
            existing.SkillLevel              = profile.SkillLevel;
            existing.PreferredStyle          = profile.PreferredStyle;
            existing.DateOfBirth             = profile.DateOfBirth;
            existing.Address                 = profile.Address;
            existing.City                    = profile.City;
            existing.FavoriteRoutes          = profile.FavoriteRoutes;
            existing.PreferenceNotes         = profile.PreferenceNotes;
            existing.EmailNotificationsEnabled = profile.EmailNotificationsEnabled;
            existing.SmsNotificationsEnabled = profile.SmsNotificationsEnabled;
            existing.UpdatedAt               = DateTime.UtcNow;
            _db.UserProfiles.Update(existing);
            profile = existing;
        }

        await _db.SaveChangesAsync(ct);
        return profile;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// EfEquipmentRepository (extended with Create + Update)
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
            .AsNoTracking()
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.OrderBy(e => e.Size).ToList(), ct);
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
// EfRentalRepository (adds GetAllRentalsAsync for reporting)
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
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .OrderBy(r => r.EndTime)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <summary>All rentals in a date range — used by reports and analytics.</summary>
    public async Task<IReadOnlyList<Rental>> GetAllRentalsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Rentals
            .Where(r => r.StartTime >= from && r.StartTime <= to)
            .Include(r => r.Customer)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
            .Include(r => r.Payments)
            .OrderByDescending(r => r.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId,
        CancellationToken ct = default)
        => await _db.Rentals
            .Where(r => r.CustomerProfileId == customerId)
            .Include(r => r.RentalEquipments)
                .ThenInclude(re => re.Equipment)
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
        GetRentalSummariesByCustomerIdsAsync(
        IEnumerable<int> customerIds,
        CancellationToken ct = default)
    {
        var ids = customerIds.ToList();

        return await _db.Rentals
            .Where(r => ids.Contains(r.CustomerProfileId))
            .GroupBy(r => r.CustomerProfileId)
            .Select(g => new
            {
                CustomerId    = g.Key,
                TotalRentals  = g.Count(),
                ActiveRentals = g.Count(r => r.Status == RentalStatus.Active
                                          || r.Status == RentalStatus.Pending),
                TotalSpent    = g.Where(r => r.PaymentStatus == PaymentStatus.Paid)
                                 .Sum(r => r.TotalAmount),
                LastRentalDate = (DateTime?)g.Max(r => r.StartTime)
            })
            .AsNoTracking()
            .ToDictionaryAsync(
                x => x.CustomerId,
                x => new CustomerRentalSummary
                {
                    TotalRentals   = x.TotalRentals,
                    ActiveRentals  = x.ActiveRentals,
                    TotalSpent     = x.TotalSpent,
                    LastRentalDate = x.LastRentalDate
                },
                ct);
    }
}

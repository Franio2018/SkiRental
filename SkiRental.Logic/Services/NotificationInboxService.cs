using Microsoft.EntityFrameworkCore;
using SkiRental.Domain.Models;

namespace SkiRental.Logic.Services;

public sealed class NotificationInboxService
{
    private readonly SkiRentalDbContext _db;

    public NotificationInboxService(SkiRentalDbContext db) => _db = db;

    public async Task<IReadOnlyList<AppNotification>> GetForUserAsync(int appUserId, CancellationToken ct = default)
        => await _db.AppNotifications
            .Where(n => n.AppUserId == appUserId || n.AppUserId == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(int appUserId, CancellationToken ct = default)
        => await _db.AppNotifications
            .CountAsync(n => !n.IsRead && (n.AppUserId == appUserId || n.AppUserId == null), ct);

    public async Task AddAsync(int? appUserId, string title, string message,
        string category = "General", int? rentalId = null, CancellationToken ct = default)
    {
        _db.AppNotifications.Add(new AppNotification
        {
            AppUserId = appUserId,
            Title = title,
            Message = message,
            Category = category,
            RelatedRentalId = rentalId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(int notificationId, CancellationToken ct = default)
    {
        var n = await _db.AppNotifications.FindAsync([notificationId], ct);
        if (n is null) return;
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(int appUserId, CancellationToken ct = default)
    {
        await _db.AppNotifications
            .Where(x => !x.IsRead && (x.AppUserId == appUserId || x.AppUserId == null))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}

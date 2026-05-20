using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkiRental.Domain.Models;

[Table("AppNotifications")]
public class AppNotification
{
    [Key]
    public int Id { get; set; }

    /// <summary>Target <see cref="AppUser"/> (null = broadcast to all staff).</summary>
    public int? AppUserId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = "General";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? RelatedRentalId { get; set; }
}

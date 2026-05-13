using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkiRental.Domain.Models;

/// <summary>
/// Authentication account stored in the database.
/// Deliberately separate from <see cref="UserProfile"/> so that physical/preference
/// data can exist independently of login credentials (e.g. walk-in customers
/// whose profile is created by a worker but who never log in themselves).
/// </summary>
[Table("AppUsers")]
public class AppUser
{
    [Key]
    public int Id { get; set; }

    // ── Credentials ──────────────────────────────────────────────────────────

    /// <summary>Login name – unique, case-insensitive (stored lowercase).</summary>
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the user's password (workFactor 12).</summary>
    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    // ── Role ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Role stored as a string so the column is human-readable in SQLite.
    /// Use <see cref="AppRole"/> enum for comparisons in code.
    /// </summary>
    [Required, MaxLength(20)]
    public string Role { get; set; } = AppRole.Customer.ToString();

    [NotMapped]
    public AppRole AppRole
    {
        get => Enum.TryParse<AppRole>(Role, out var r) ? r : AppRole.Customer;
        set => Role = value.ToString();
    }

    // ── Profile link ─────────────────────────────────────────────────────────

    /// <summary>
    /// Optional FK to <see cref="UserProfile"/>.
    /// Null = account exists but physical profile not yet filled in.
    /// </summary>
    public int? UserProfileId { get; set; }

    public UserProfile? Profile { get; set; }

    // ── Security ─────────────────────────────────────────────────────────────

    /// <summary>Locked accounts cannot log in regardless of password.</summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>Consecutive failed login attempts (reset on success).</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>Lock expires at this UTC time (null = permanent lock).</summary>
    public DateTime? LockoutEnd { get; set; }

    // ── Audit ────────────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Returns true if the account is currently locked out.</summary>
    [NotMapped]
    public bool IsCurrentlyLocked =>
        IsLocked && (LockoutEnd is null || LockoutEnd > DateTime.UtcNow);
}

/// <summary>
/// Application roles – ordered by privilege level ascending.
/// Owner > Worker > Customer.
/// </summary>
public enum AppRole
{
    Customer = 0,
    Worker   = 1,
    Owner    = 2
}

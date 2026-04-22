using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkiRental.Domain.Enums;

namespace SkiRental.Domain.Models;

/// <summary>
/// Dane osobowe i fizyczne klienta.
/// </summary>
[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    public int Id { get; set; }

    // ── Dane identyfikacyjne ─────────────────────────────────────────────────
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    // ── Dane fizyczne (PhysicalData) ────────────────────────────────────────
    /// <summary>Wzrost [cm].</summary>
    [Range(100, 250)]
    public int Height { get; set; }

    /// <summary>Waga [kg].</summary>
    [Column(TypeName = "decimal(5,1)")]
    [Range(30, 300)]
    public decimal Weight { get; set; }

    /// <summary>Rozmiar buta (EU).</summary>
    [Range(30, 55)]
    public int ShoeSize { get; set; }

    /// <summary>Poziom zaawansowania.</summary>
    public SkillLevel SkillLevel { get; set; } = SkillLevel.Beginner;

    /// <summary>Preferowany styl jazdy.</summary>
    public SkiStyle PreferredStyle { get; set; } = SkiStyle.Classic;

    /// <summary>Wyliczony BMI (właściwość obliczana – nie mapowana do kolumny).</summary>
    [NotMapped]
    public decimal Bmi => Height > 0
        ? Math.Round(Weight / ((decimal)(Height / 100.0) * (decimal)(Height / 100.0)), 1)
        : 0;

    /// <summary>Czy BMI jest w normie (18.5–24.9).</summary>
    [NotMapped]
    public bool IsBmiNormal => Bmi is >= 18.5m and <= 24.9m;

    /// <summary>Czy klient jest poniżej normy BMI (< 18.5).</summary>
    [NotMapped]
    public bool IsUnderweight => Bmi < 18.5m;

    /// <summary>Czy klient jest powyżej normy BMI (> 24.9).</summary>
    [NotMapped]
    public bool IsOverweight => Bmi > 24.9m;

    // ── Systemowe ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<Rental> Rentals { get; set; } = [];
}

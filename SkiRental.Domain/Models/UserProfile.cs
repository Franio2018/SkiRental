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

    // ── Dane osobiste dodatkowe ──────────────────────────────────────────────
    /// <summary>Data urodzenia.</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Peletny wiek (obliczany).</summary>
    [NotMapped]
    public int? Age => DateOfBirth.HasValue ? DateTime.Today.Year - DateOfBirth.Value.Year - (DateTime.Today.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0) : null;

    /// <summary>Adres zamieszkania.</summary>
    [MaxLength(500)]
    public string? Address { get; set; }

    /// <summary>Miasto.</summary>
    [MaxLength(100)]
    public string? City { get; set; }

    // Preferencje i ustawienia ────────────────────────────────────────────────
    /// <summary>Zdjecie profilowe (sciezka do pliku).</summary>
    [MaxLength(500)]
    public string? ProfileImagePath { get; set; }

    /// <summary>Preferowane trasy narciarskie.</summary>
    [MaxLength(1000)]
    public string? FavoriteRoutes { get; set; }

    /// <summary>Notatki o preferencjach klienta.</summary>
    [MaxLength(2000)]
    public string? PreferenceNotes { get; set; }

    /// <summary>Czy chce otrzymywac powiadomienia email.</summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>Czy chce otrzymywac powiadomienia SMS.</summary>
    public bool SmsNotificationsEnabled { get; set; } = false;

    // Statystyki i historie ────────────────────────────────────────────────────
    /// <summary>Laczna liczba wypozyczen.</summary>
    [NotMapped]
    public int TotalRentalsCount => Rentals?.Count ?? 0;

    /// <summary>Data ostatniego wypozyczenia.</summary>
    [NotMapped]
    public DateTime? LastRentalDate => Rentals?.OrderByDescending(r => r.StartTime).FirstOrDefault()?.StartTime;

    /// <summary>Laczna kwota wydana na wypozyczenia.</summary>
    [NotMapped]
    public decimal TotalSpent => Rentals?.Where(r => r.PaymentStatus == PaymentStatus.Paid).Sum(r => r.TotalAmount) ?? 0;

    /// <summary>Laczna liczba dni na nartach (suma dni ze wszystkich nieanulowanych wypozyczen).</summary>
    [NotMapped]
    public int TotalSkiDays => Rentals?
        .Where(r => r.Status != RentalStatus.Cancelled)
        .Sum(RentalSkiDays) ?? 0;

    private static int RentalSkiDays(Rental r)
    {
        var end = r.ActualReturnTime ?? r.EndTime;
        if (end < r.StartTime) return 0;
        return Math.Max(1, (end.Date - r.StartTime.Date).Days + 1);
    }

    /// <summary>Ulubiona kategoria sprzetu (najczesciej wypozyczana).</summary>
    [NotMapped]
    public EquipmentCategory? FavoriteEquipmentCategory => Rentals?
        .SelectMany(r => r.RentalEquipments)
        .GroupBy(re => re.Equipment.Category)
        .OrderByDescending(g => g.Count())
        .FirstOrDefault()?.Key;

    // ── Systemowe ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<Rental> Rentals { get; set; } = [];
}

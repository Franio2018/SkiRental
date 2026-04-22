using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkiRental.Domain.Enums;

namespace SkiRental.Domain.Models;

/// <summary>
/// Reprezentuje pojedynczy egzemplarz sprzętu w wypożyczalni.
/// </summary>
[Table("Equipment")]
public class Equipment
{
    [Key]
    public int Id { get; set; }

    /// <summary>Unikalny numer seryjny producenta.</summary>
    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>Marka / producent.</summary>
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    /// <summary>Model sprzętu.</summary>
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    public EquipmentCategory Category { get; set; }

    /// <summary>Długość nart [cm] lub rozmiar butów.</summary>
    [Column(TypeName = "decimal(5,1)")]
    public decimal Size { get; set; }

    /// <summary>Sztywność narty (Flex Index 1–120).</summary>
    public int? Stiffness { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    public SkiStyle? CompatibleStyle { get; set; }

    /// <summary>Rok produkcji.</summary>
    public int YearOfProduction { get; set; }

    /// <summary>Stawka dzienna wynajmu [PLN].</summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal DailyRate { get; set; }

    /// <summary>Łączna liczba dni wypożyczeń (do analizy użycia).</summary>
    public int TotalRentalDays { get; set; }

    /// <summary>Ścieżka do zdjęcia sprzętu.</summary>
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    /// <summary>Data ostatniego przeglądu serwisowego.</summary>
    public DateTime? LastMaintenanceDate { get; set; }

    /// <summary>Notatki serwisowe.</summary>
    [MaxLength(1000)]
    public string? MaintenanceNotes { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<RentalEquipment> RentalEquipments { get; set; } = [];
}

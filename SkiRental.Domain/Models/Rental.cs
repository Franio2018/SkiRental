using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkiRental.Domain.Enums;

namespace SkiRental.Domain.Models;

/// <summary>
/// Wypożyczenie – główny agregat systemu.
/// </summary>
[Table("Rentals")]
public class Rental
{
    [Key]
    public int Id { get; set; }

    // ── Relacje ──────────────────────────────────────────────────────────────
    public int CustomerProfileId { get; set; }
    public UserProfile Customer { get; set; } = null!;

    /// <summary>Pracownik obsługujący wypożyczenie.</summary>
    public int? EmployeeId { get; set; }

    // ── Czas ─────────────────────────────────────────────────────────────────
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? ActualReturnTime { get; set; }

    /// <summary>Planowany czas trwania [h].</summary>
    [NotMapped]
    public double PlannedDurationHours => (EndTime - StartTime).TotalHours;

    // ── QR / NFC ─────────────────────────────────────────────────────────────
    /// <summary>Unikalny identyfikator QR/NFC dla potwierdzenia zwrotu.</summary>
    [MaxLength(500)]
    public string? QrCode { get; set; }

    /// <summary>Zawartość tagu NFC zakodowana jako Base64.</summary>
    [MaxLength(1000)]
    public string? NfcTagData { get; set; }

    // ── Płatność ─────────────────────────────────────────────────────────────
    public bool IsPreauthorized { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PreauthorizedAmount { get; set; }

    /// <summary>ID transakcji PayU.</summary>
    [MaxLength(200)]
    public string? PayUOrderId { get; set; }

    // ── Status ───────────────────────────────────────────────────────────────
    public RentalStatus Status { get; set; } = RentalStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // ── Systemowe ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ──────────────────────────────────────────────────────────
    public ICollection<RentalEquipment> RentalEquipments { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

/// <summary>
/// Tabela łącząca Rental ↔ Equipment (wiele-do-wielu).
/// </summary>
[Table("RentalEquipments")]
public class RentalEquipment
{
    public int RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;

    /// <summary>Stan sprzętu przy wydaniu (1–5).</summary>
    public int ConditionAtPickup { get; set; } = 5;

    /// <summary>Stan sprzętu przy zwrocie (1–5).</summary>
    public int? ConditionAtReturn { get; set; }
}

/// <summary>
/// Rejestr transakcji płatniczych.
/// </summary>
[Table("Payments")]
public class Payment
{
    [Key]
    public int Id { get; set; }

    public int RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    [MaxLength(200)]
    public string? ExternalTransactionId { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    [MaxLength(1000)]
    public string? ProviderResponse { get; set; }
}

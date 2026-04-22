using SkiRental.Domain.Models;

namespace SkiRental.Logic.Interfaces;

// ═══════════════════════════════════════════════════════════════════════════════
// IPaymentProvider – kontrakt dla dowolnego dostawcy płatności
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interfejs abstrakcji płatności – pozwala podmieniać PayU na innego dostawcę
/// bez zmiany logiki biznesowej (Open/Closed Principle).
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Inicjuje płatność BLIK (6-cyfrowy kod).
    /// </summary>
    /// <param name="amount">Kwota w PLN.</param>
    /// <param name="blikCode">6-cyfrowy kod BLIK podany przez klienta.</param>
    /// <param name="orderId">ID zamówienia w systemie wypożyczalni.</param>
    /// <param name="description">Opis transakcji (widoczny w historii klienta).</param>
    /// <returns>Wynik inicjowania płatności.</returns>
    Task<PaymentResult> InitiateBlikPayment(decimal amount, string blikCode,
        string orderId, string description);

    /// <summary>
    /// Tworzy wstępną autoryzację (pre-auth) bez obciążania konta.
    /// </summary>
    Task<PaymentResult> PreauthorizePayment(decimal amount, string orderId, string description);

    /// <summary>
    /// Finalizuje wcześniej preautoryzowaną płatność.
    /// </summary>
    Task<PaymentResult> CapturePreauthorization(string externalTransactionId, decimal amount);

    /// <summary>
    /// Anuluje preautoryzację lub dokonuje zwrotu.
    /// </summary>
    Task<PaymentResult> RefundPayment(string externalTransactionId, decimal? partialAmount = null);

    /// <summary>
    /// Sprawdza aktualny status transakcji.
    /// </summary>
    Task<PaymentResult> CheckPaymentStatus(string externalTransactionId);
}

// ═══════════════════════════════════════════════════════════════════════════════
// PaymentResult DTO
// ═══════════════════════════════════════════════════════════════════════════════

public record PaymentResult
{
    public bool IsSuccess { get; init; }
    public string? ExternalTransactionId { get; init; }
    public string? Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ProviderRawResponse { get; init; }

    public static PaymentResult Success(string transactionId, decimal amount, string status = "COMPLETED") =>
        new() { IsSuccess = true, ExternalTransactionId = transactionId, Amount = amount, Status = status };

    public static PaymentResult Failure(string errorCode, string errorMessage) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

// ═══════════════════════════════════════════════════════════════════════════════
// IEquipmentRepository
// ═══════════════════════════════════════════════════════════════════════════════

public interface IEquipmentRepository
{
    Task<IReadOnlyList<Equipment>> GetAvailableByStyleAsync(
        SkiRental.Domain.Enums.SkiStyle style,
        SkiRental.Domain.Enums.EquipmentCategory category,
        CancellationToken ct = default);

    Task<Equipment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken ct = default);
    Task UpdateStatusAsync(int equipmentId, SkiRental.Domain.Enums.EquipmentStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Equipment>> GetByRentalIdAsync(int rentalId, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IRentalRepository
// ═══════════════════════════════════════════════════════════════════════════════

public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Rental?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default);
    Task UpdateAsync(Rental rental, CancellationToken ct = default);

    Task<IReadOnlyList<Rental>> GetRentalsForUtilizationAsync(
        int equipmentId, DateTime seasonStart, DateTime seasonEnd, CancellationToken ct = default);
}

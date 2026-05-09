using SkiRental.Domain.Models;

namespace SkiRental.Logic.Interfaces;

// ═══════════════════════════════════════════════════════════════════════════════
// IUserRepository — auth + profile persistence
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Persistence contract for <see cref="AppUser"/> (credentials) and the
/// linked <see cref="UserProfile"/> (physical data).
/// </summary>
public interface IUserRepository
{
    /// <summary>Find an auth account by username (case-insensitive).</summary>
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Find an auth account by its primary key.</summary>
    Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Returns all accounts – for owner staff management panel.</summary>
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Persist a new account (username must be unique).</summary>
    Task<AppUser> CreateAsync(AppUser user, CancellationToken ct = default);

    /// <summary>Persist changes to an existing account (role, lock, password hash).</summary>
    Task UpdateAsync(AppUser user, CancellationToken ct = default);

    /// <summary>Permanently delete an account (Owner-only action).</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Returns true when the username is already taken.</summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    // ── Profile ───────────────────────────────────────────────────────────────

    /// <summary>Get the UserProfile linked to an AppUser (may be null).</summary>
    Task<UserProfile?> GetProfileByUserIdAsync(int appUserId, CancellationToken ct = default);

    /// <summary>Create or update the UserProfile and link it to the AppUser.</summary>
    Task<UserProfile> UpsertProfileAsync(int appUserId, UserProfile profile, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IPaymentProvider – payment gateway abstraction (unchanged)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interfejs abstrakcji płatności – pozwala podmieniać PayU na innego dostawcę
/// bez zmiany logiki biznesowej (Open/Closed Principle).
/// </summary>
public interface IPaymentProvider
{
    Task<PaymentResult> InitiateBlikPayment(decimal amount, string blikCode,
        string orderId, string description);

    Task<PaymentResult> PreauthorizePayment(decimal amount, string orderId, string description);

    Task<PaymentResult> CapturePreauthorization(string externalTransactionId, decimal amount);

    Task<PaymentResult> RefundPayment(string externalTransactionId, decimal? partialAmount = null);

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

    /// <summary>Persist a new equipment item.</summary>
    Task<Equipment> CreateAsync(Equipment equipment, CancellationToken ct = default);

    /// <summary>Update an existing equipment item (brand, model, rates, maintenance).</summary>
    Task UpdateAsync(Equipment equipment, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IRentalRepository
// ═══════════════════════════════════════════════════════════════════════════════

public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Rental?> GetByQrCodeAsync(string qrCode, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetActiveRentalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetAllRentalsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Rental>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Rental> CreateAsync(Rental rental, CancellationToken ct = default);
    Task UpdateAsync(Rental rental, CancellationToken ct = default);

    Task<IReadOnlyList<Rental>> GetRentalsForUtilizationAsync(
        int equipmentId, DateTime seasonStart, DateTime seasonEnd, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IAuthenticationService – authentication abstraction
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Authentication service interface for UI components.
/// Provides current user session information without exposing implementation details.
/// </summary>
public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string? CurrentUsername { get; }
    int? CurrentUserId { get; }
    AppRole CurrentRole { get; }
    string? CurrentJwtToken { get; }
    
    event Action? AuthenticationChanged;
}

using SkiRental.Domain.Models;
using SkiRental.Domain.Enums;

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

    /// <summary>Check if equipment exists by ID.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
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

    /// <summary>
    /// Zwraca podsumowanie wypożyczeń pogrupowane po ID klienta.
    /// Używane w panelu zarządczym zamiast N osobnych zapytań.
    /// </summary>
    Task<IReadOnlyDictionary<int, CustomerRentalSummary>> GetRentalSummariesByCustomerIdsAsync(
        IEnumerable<int> customerIds,
        CancellationToken ct = default);
}

public record CustomerRentalSummary
{
    public int TotalRentals   { get; init; }
    public int ActiveRentals  { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime? LastRentalDate { get; init; }
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

// ═══════════════════════════════════════════════════════════════════════════════
// IRecommendationOrchestrator – enhanced recommendation service
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Enhanced recommendation service that builds on EquipmentMathService.
/// Handles per-category selection, fallback logic, and no-match detection.
/// </summary>
public interface IRecommendationOrchestrator
{
    Task<RecommendationResult> RecommendAsync(EquipmentSelectionParameters parameters, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// IGearSwapService – customer-facing gear swap alternatives
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Service for providing customer-facing gear swap options.
/// Returns scored and filtered alternatives within safety tolerance.
/// </summary>
public interface IGearSwapService
{
    Task<List<SwapOption>> GetSwapOptionsAsync(
        EquipmentCategory category,
        EquipmentSelectionParameters parameters,
        int excludeEquipmentId,
        CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// INotificationService – customer notifications (worker overrides, etc.)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Notification service for sending customer alerts (email/SMS).
/// Used for worker override notifications and other customer communications.
/// </summary>
public interface INotificationService
{
    Task SendEmailAsync(string to, NotificationPayload payload, CancellationToken ct = default);
    Task SendSmsAsync(string phone, NotificationPayload payload, CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════════════
// NotificationPayload DTO
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Payload for customer notifications.
/// </summary>
public record NotificationPayload
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? SmsShortMessage { get; init; }
    public Dictionary<string, string> TemplateData { get; init; } = new();
}

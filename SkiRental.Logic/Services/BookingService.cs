using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Zarządza cyklem życia wypożyczenia:
///   Pending (reserved) → Active (equipment picked up) → Completed (returned)
///
/// FIX vs original: equipment status is set to Reserved on booking, then Rented
/// only on activation (actual pickup). This prevents phantom locks when payment
/// fails or customer never shows.
/// </summary>
public sealed class BookingService
{
    private readonly IRentalRepository    _rentalRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IPaymentProvider     _paymentProvider;
    private readonly RentalRulesService   _rules;
    private readonly NotificationInboxService _notifications;
    private readonly SkiRentalDbContext  _db;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IRentalRepository rentalRepo,
        IEquipmentRepository equipmentRepo,
        IPaymentProvider paymentProvider,
        RentalRulesService rules,
        NotificationInboxService notifications,
        SkiRentalDbContext db,
        ILogger<BookingService> logger)
    {
        _rentalRepo      = rentalRepo;
        _equipmentRepo   = equipmentRepo;
        _paymentProvider = paymentProvider;
        _rules           = rules;
        _notifications   = notifications;
        _db              = db;
        _logger          = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 1. Create booking (Pending)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a reservation, marks equipment as Reserved (not Rented yet)
    /// and optionally takes BLIK payment upfront.
    /// Equipment only moves to Rented when <see cref="ActivateRentalAsync"/> is called
    /// (i.e. when the worker hands over the equipment to the customer).
    /// 
    /// Uses atomic transaction with ExecuteUpdateAsync to prevent race conditions.
    /// </summary>
    public async Task<BookingResult> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating booking for customer {CustomerId}", request.CustomerProfileId);

        var windowCheck = _rules.ValidateBookingWindow(request.StartTime, request.EndTime);
        if (!windowCheck.IsValid)
            return BookingResult.Failure(windowCheck.Error!);

        // Używamy jawnej transakcji żeby sprawdzenie + zmiana statusu były atomowe
        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        try
        {
            // ── 1. ATOMOWE zajęcie sprzętu ──────────────────────────────────────
            // ExecuteUpdateAsync z WHERE Status = Available zwróci liczbę
            // faktycznie zaktualizowanych wierszy. Jeśli 0 – sprzęt właśnie
            // zajął ktoś inny (race condition przechwycony).

            foreach (var equipmentId in request.EquipmentIds)
            {
                // Najpierw sprawdź czy sprzęt w ogóle istnieje
                var exists = await _equipmentRepo.ExistsAsync(equipmentId, ct);
                if (!exists)
                    return BookingResult.Failure($"Sprzęt #{equipmentId} nie istnieje.");

                // Atomowo zmień Available → Reserved TYLKO jeśli nadal Available
                var affected = await _db.Equipment
                    .Where(e => e.Id == equipmentId
                             && e.Status == EquipmentStatus.Available)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(e => e.Status, EquipmentStatus.Reserved),
                        ct);

                if (affected == 0)
                {
                    // Wycofaj już zmienione wiersze w tej transakcji
                    await tx.RollbackAsync(ct);

                    var eq = await _equipmentRepo.GetByIdAsync(equipmentId, ct);
                    return BookingResult.Failure(
                        $"Sprzęt {eq?.SerialNumber ?? $"#{equipmentId}"} " +
                        $"jest niedostępny ({eq?.Status}). Inny klient zarezerwował go właśnie teraz.");
                }
            }

            // ── 2. Oblicz kwotę ─────────────────────────────────────────────────
            var totalAmount = await CalculateTotalAmountAsync(request, ct);
            var qrCode = GenerateQrCode(request);

            // ── 3. Utwórz encję Rental ───────────────────────────────────────────
            var rental = new Rental
            {
                CustomerProfileId = request.CustomerProfileId,
                StartTime         = request.StartTime,
                EndTime           = request.EndTime,
                TotalAmount       = totalAmount,
                QrCode            = qrCode,
                Status            = RentalStatus.Pending,
                PaymentStatus     = PaymentStatus.Unpaid,
                Notes             = request.Notes,
                RentalEquipments  = request.EquipmentIds
                    .Select(id => new RentalEquipment { EquipmentId = id, ConditionAtPickup = 5 })
                    .ToList()
            };

            rental = await _rentalRepo.CreateAsync(rental, ct);

            // ── 4. Opcjonalna płatność BLIK lub preautoryzacja karty ─────────────
            if (request.BlikCode is not null)
            {
                var payResult = await _paymentProvider.InitiateBlikPayment(
                    totalAmount, request.BlikCode,
                    rental.Id.ToString(), BuildOrderDescription(rental));

                if (payResult.IsSuccess)
                {
                    rental.PaymentStatus   = PaymentStatus.Paid;
                    rental.PayUOrderId     = payResult.ExternalTransactionId;
                    rental.IsPreauthorized = false;
                    await _rentalRepo.UpdateAsync(rental, ct);
                }
                else
                {
                    _logger.LogWarning("BLIK payment failed: {Error}", payResult.ErrorMessage);
                    await tx.RollbackAsync(ct);
                    await RevertReservedEquipmentAsync(request.EquipmentIds, ct);
                    return BookingResult.Failure(
                        $"Płatność BLIK nie powiodła się: {payResult.ErrorMessage}",
                        rental.Id, qrCode);
                }
            }
            else if (request.UseCardPreauth)
            {
                var preAuth = await _paymentProvider.PreauthorizePayment(
                    totalAmount, rental.Id.ToString(), BuildOrderDescription(rental));

                if (preAuth.IsSuccess)
                {
                    rental.PaymentStatus        = PaymentStatus.Preauthorized;
                    rental.PayUOrderId          = preAuth.ExternalTransactionId;
                    rental.IsPreauthorized      = true;
                    rental.PreauthorizedAmount  = totalAmount;
                    await _rentalRepo.UpdateAsync(rental, ct);
                }
                else
                {
                    await tx.RollbackAsync(ct);
                    await RevertReservedEquipmentAsync(request.EquipmentIds, ct);
                    return BookingResult.Failure(
                        $"Preautoryzacja karty nie powiodła się: {preAuth.ErrorMessage}",
                        rental.Id, qrCode);
                }
            }

            await tx.CommitAsync(ct);
            _logger.LogInformation("Booking {RentalId} created. QR={QrCode}", rental.Id, qrCode);

            await _notifications.AddAsync(
                null,
                "Nowa rezerwacja",
                $"Rezerwacja #{rental.Id} – {totalAmount:N2} PLN. QR: {qrCode}",
                "Booking",
                rental.Id,
                ct);

            return BookingResult.Success(rental.Id, qrCode, totalAmount);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            // Sprzęt automatycznie wraca do stanu sprzed transakcji (rollback)
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2. Activate rental (worker hands over equipment)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Transitions a Pending rental to Active.
    /// This is the moment equipment physically leaves the shop — equipment
    /// status is updated to Rented here, not at booking time.
    /// Called by the worker panel after verifying customer identity.
    /// </summary>
    public async Task<ActivationResult> ActivateRentalAsync(
        int rentalId,
        int workerId,
        CancellationToken ct = default)
    {
        var rental = await _rentalRepo.GetByIdAsync(rentalId, ct);

        if (rental is null)
            return ActivationResult.Failure($"Wypożyczenie #{rentalId} nie istnieje.");

        if (rental.Status != RentalStatus.Pending)
            return ActivationResult.Failure(
                $"Wypożyczenie #{rentalId} ma status {rental.Status}. Aktywacja niemożliwa.");

        // Spot payment: worker collects cash/card at pickup → mark paid on activation
        if (rental.PaymentStatus == PaymentStatus.Unpaid)
            rental.PaymentStatus = PaymentStatus.Paid;

        // Mark equipment as physically Rented
        foreach (var re in rental.RentalEquipments)
            await _equipmentRepo.UpdateStatusAsync(re.EquipmentId, EquipmentStatus.Rented, ct);

        rental.Status     = RentalStatus.Active;
        rental.EmployeeId = workerId;
        await _rentalRepo.UpdateAsync(rental, ct);

        _logger.LogInformation("Rental {RentalId} activated by worker {WorkerId}", rentalId, workerId);
        return ActivationResult.Success(rentalId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 3. Process return by QR code
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Processes equipment return when worker scans the customer's QR code.
    /// Records condition at return, releases equipment back to Available,
    /// and finalises pre-authorised payment if applicable.
    /// </summary>
    public async Task<ReturnResult> ProcessReturnByQrCodeAsync(
        string qrCode,
        Dictionary<int, int> equipmentConditions,     // equipmentId → condition 1–5
        CancellationToken ct = default)
    {
        var rental = await _rentalRepo.GetByQrCodeAsync(qrCode, ct);

        if (rental is null)
            return ReturnResult.Failure("Nie znaleziono wypożyczenia dla podanego kodu QR.");

        if (rental.Status != RentalStatus.Active)
            return ReturnResult.Failure(
                $"Wypożyczenie ma status {rental.Status}. Zwrot niemożliwy.");

        // Record condition at return and release equipment
        foreach (var re in rental.RentalEquipments)
        {
            if (equipmentConditions.TryGetValue(re.EquipmentId, out var condition))
                re.ConditionAtReturn = condition;

            await _equipmentRepo.UpdateStatusAsync(re.EquipmentId, EquipmentStatus.Available, ct);
        }

        var returnTime = DateTime.UtcNow;
        rental.Status           = RentalStatus.Completed;
        rental.ActualReturnTime = returnTime;

        var lateFee = _rules.CalculateLateFee(rental.EndTime, returnTime, rental.TotalAmount);
        var worstCondition = rental.RentalEquipments
            .Where(re => re.ConditionAtReturn.HasValue)
            .Select(re => re.ConditionAtReturn!.Value)
            .DefaultIfEmpty(5)
            .Min();
        var damageFee = _rules.CalculateDamageSurcharge(rental.TotalAmount, worstCondition);
        var extraCharges = lateFee + damageFee;
        var captureAmount = rental.TotalAmount + extraCharges;

        // Capture pre-auth if applicable
        if (rental.IsPreauthorized && rental.PayUOrderId is not null)
        {
            var captureResult = await _paymentProvider.CapturePreauthorization(
                rental.PayUOrderId, captureAmount);
            if (captureResult.IsSuccess)
                rental.PaymentStatus = PaymentStatus.Paid;
        }
        else if (extraCharges > 0 && rental.PaymentStatus == PaymentStatus.Paid)
        {
            rental.TotalAmount += extraCharges;
        }

        await _rentalRepo.UpdateAsync(rental, ct);

        _logger.LogInformation("Return processed for rental {RentalId}", rental.Id);

        var damagedItems = rental.RentalEquipments
            .Where(re => re.ConditionAtReturn.HasValue && re.ConditionAtReturn < 3)
            .Select(re => re.EquipmentId)
            .ToList();

        if (lateFee > 0 || damageFee > 0)
        {
            await _notifications.AddAsync(
                null,
                "Dopłaty przy zwrocie",
                $"Wypożyczenie #{rental.Id}: opóźnienie {lateFee:N2} PLN, uszkodzenia {damageFee:N2} PLN.",
                "Return",
                rental.Id,
                ct);
        }

        return ReturnResult.Success(rental.Id, returnTime, damagedItems, lateFee, damageFee);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 4. Cancel booking
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cancels a Pending booking and (if already paid) initiates a refund.
    /// Cannot cancel Active rentals — use return flow instead.
    /// </summary>
    public async Task<CancelResult> CancelBookingAsync(
        int rentalId,
        CancellationToken ct = default)
    {
        var rental = await _rentalRepo.GetByIdAsync(rentalId, ct);

        if (rental is null)
            return CancelResult.Failure($"Wypożyczenie #{rentalId} nie istnieje.");

        if (rental.Status == RentalStatus.Active)
            return CancelResult.Failure("Nie można anulować aktywnego wypożyczenia. Użyj procesu zwrotu.");

        if (rental.Status is RentalStatus.Completed or RentalStatus.Cancelled)
            return CancelResult.Failure($"Wypożyczenie ma już status {rental.Status}.");

        // Refund if already paid
        string? refundTxId = null;
        if (rental.PaymentStatus == PaymentStatus.Paid && rental.PayUOrderId is not null)
        {
            var refund = await _paymentProvider.RefundPayment(rental.PayUOrderId);
            if (refund.IsSuccess)
            {
                rental.PaymentStatus = PaymentStatus.Refunded;
                refundTxId = refund.ExternalTransactionId;
            }
            else
            {
                _logger.LogWarning("Refund failed for rental {RentalId}: {Error}", rentalId, refund.ErrorMessage);
                // Don't block cancellation — log and continue; finance team can handle manually
            }
        }

        foreach (var re in rental.RentalEquipments)
            await _equipmentRepo.UpdateStatusAsync(re.EquipmentId, EquipmentStatus.Available, ct);

        rental.Status = RentalStatus.Cancelled;
        await _rentalRepo.UpdateAsync(rental, ct);

        _logger.LogInformation("Rental {RentalId} cancelled. RefundTx={RefundTxId}", rentalId, refundTxId);
        return CancelResult.Success(rentalId, refundTxId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private async Task RevertReservedEquipmentAsync(
        IEnumerable<int> equipmentIds,
        CancellationToken ct)
    {
        foreach (var equipmentId in equipmentIds)
            await _equipmentRepo.UpdateStatusAsync(equipmentId, EquipmentStatus.Available, ct);
    }

    private async Task<decimal> CalculateTotalAmountAsync(CreateBookingRequest r, CancellationToken ct)
    {
        decimal total = 0;
        var hours = (decimal)(r.EndTime - r.StartTime).TotalHours;

        foreach (var id in r.EquipmentIds)
        {
            var eq = await _equipmentRepo.GetByIdAsync(id, ct);
            if (eq is not null)
                total += eq.DailyRate * (hours / 24m);
        }

        return Math.Round(total, 2);
    }

    private static string GenerateQrCode(CreateBookingRequest r) =>
        $"SKI-{r.CustomerProfileId:D6}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40];

    private static string BuildOrderDescription(Rental r) =>
        $"Wynajem nart – klient #{r.CustomerProfileId} – {r.StartTime:dd.MM.yyyy} do {r.EndTime:dd.MM.yyyy}";

    /// <summary>
    /// Returns a rental with all related data loaded by its QR code.
    /// Used by the employee QR scanner panel.
    /// </summary>
    public Task<Rental?> GetRentalByQrCodeAsync(string qrCode, CancellationToken ct = default)
        => _rentalRepo.GetByQrCodeAsync(qrCode, ct);

}

// ═══════════════════════════════════════════════════════════════════════════════
// Request / Result DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public record CreateBookingRequest
{
    public int CustomerProfileId { get; init; }
    public IReadOnlyList<int> EquipmentIds { get; init; } = [];
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string? BlikCode { get; init; }
    public bool UseCardPreauth { get; init; }
    public string? Notes { get; init; }
}

public record BookingResult
{
    public bool IsSuccess { get; init; }
    public int? RentalId { get; init; }
    public string? QrCode { get; init; }
    public decimal? TotalAmount { get; init; }
    public string? ErrorMessage { get; init; }

    public static BookingResult Success(int rentalId, string qrCode, decimal amount) =>
        new() { IsSuccess = true, RentalId = rentalId, QrCode = qrCode, TotalAmount = amount };

    public static BookingResult Failure(string msg, int? rentalId = null, string? qrCode = null) =>
        new() { IsSuccess = false, ErrorMessage = msg, RentalId = rentalId, QrCode = qrCode };
}

public record ReturnResult
{
    public bool IsSuccess { get; init; }
    public int? RentalId { get; init; }
    public DateTime? ReturnTime { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<int> DamagedEquipmentIds { get; init; } = [];
    public decimal LateFeeAmount { get; init; }
    public decimal DamageSurchargeAmount { get; init; }

    public static ReturnResult Success(int id, DateTime t, IReadOnlyList<int> damaged,
        decimal lateFee = 0, decimal damageFee = 0) =>
        new()
        {
            IsSuccess = true,
            RentalId = id,
            ReturnTime = t,
            DamagedEquipmentIds = damaged,
            LateFeeAmount = lateFee,
            DamageSurchargeAmount = damageFee
        };

    public static ReturnResult Failure(string msg) =>
        new() { IsSuccess = false, ErrorMessage = msg };
}

public record ActivationResult
{
    public bool IsSuccess { get; init; }
    public int? RentalId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ActivationResult Success(int id) => new() { IsSuccess = true, RentalId = id };
    public static ActivationResult Failure(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
}

public record CancelResult
{
    public bool IsSuccess { get; init; }
    public int? RentalId { get; init; }
    public string? RefundTransactionId { get; init; }
    public string? ErrorMessage { get; init; }

    public static CancelResult Success(int id, string? refundTxId) =>
        new() { IsSuccess = true, RentalId = id, RefundTransactionId = refundTxId };

    public static CancelResult Failure(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
}

// ── QrScannerPanel helper ────────────────────────────────────────────────────
// Extension to BookingService added alongside QrScannerPanel refactor.
// Exposes rental lookup by QR code for the employee panel.

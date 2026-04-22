using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Zarządza cyklem życia wypożyczenia: tworzenie → preautoryzacja → aktywacja → zwrot.
/// </summary>
public sealed class BookingService
{
    private readonly IRentalRepository   _rentalRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IPaymentProvider    _paymentProvider;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IRentalRepository rentalRepo,
        IEquipmentRepository equipmentRepo,
        IPaymentProvider paymentProvider,
        ILogger<BookingService> logger)
    {
        _rentalRepo      = rentalRepo;
        _equipmentRepo   = equipmentRepo;
        _paymentProvider = paymentProvider;
        _logger          = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Tworzenie rezerwacji
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tworzy nową rezerwację, rezerwuje sprzęt i (opcjonalnie) preautoryzuje płatność.
    /// </summary>
    public async Task<BookingResult> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating booking for customer {CustomerId}", request.CustomerProfileId);

        // 1. Sprawdź dostępność każdego sprzętu
        foreach (var equipmentId in request.EquipmentIds)
        {
            var equipment = await _equipmentRepo.GetByIdAsync(equipmentId, ct)
                ?? throw new InvalidOperationException($"Sprzęt {equipmentId} nie istnieje.");

            if (equipment.Status != EquipmentStatus.Available)
                return BookingResult.Failure($"Sprzęt {equipment.SerialNumber} jest niedostępny.");
        }

        // 2. Oblicz całkowitą kwotę
        var totalAmount = await CalculateTotalAmountAsync(request, ct);

        // 3. Generuj unikalny kod QR
        var qrCode = GenerateQrCode(request);

        // 4. Utwórz encję Rental
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

        // 5. Zmień status sprzętu na "wypożyczony"
        foreach (var id in request.EquipmentIds)
            await _equipmentRepo.UpdateStatusAsync(id, EquipmentStatus.Rented, ct);

        // 6. Preautoryzacja BLIK (opcjonalna)
        if (request.BlikCode is not null)
        {
            var payResult = await _paymentProvider.InitiateBlikPayment(
                totalAmount, request.BlikCode, rental.Id.ToString(), BuildOrderDescription(rental));

            rental.PaymentStatus     = payResult.IsSuccess ? PaymentStatus.Paid : PaymentStatus.Failed;
            rental.PayUOrderId       = payResult.ExternalTransactionId;
            rental.IsPreauthorized   = payResult.IsSuccess;
            await _rentalRepo.UpdateAsync(rental, ct);

            if (!payResult.IsSuccess)
            {
                _logger.LogWarning("BLIK payment failed: {Error}", payResult.ErrorMessage);
                return BookingResult.Failure($"Płatność BLIK nie powiodła się: {payResult.ErrorMessage}",
                    rental.Id, qrCode);
            }
        }

        _logger.LogInformation("Booking {RentalId} created. QR={QrCode}", rental.Id, qrCode);
        return BookingResult.Success(rental.Id, qrCode, totalAmount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Zwrot sprzętu (przez QR / NFC)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Przetwarza zwrot sprzętu na podstawie kodu QR zeskanowanego przez pracownika.
    /// </summary>
    public async Task<ReturnResult> ProcessReturnByQrCodeAsync(
        string qrCode,
        Dictionary<int, int> equipmentConditions,
        CancellationToken ct = default)
    {
        var rental = await _rentalRepo.GetByQrCodeAsync(qrCode, ct);

        if (rental is null)
            return ReturnResult.Failure("Nie znaleziono wypożyczenia dla podanego kodu QR.");

        if (rental.Status != RentalStatus.Active)
            return ReturnResult.Failure($"Wypożyczenie ma status {rental.Status}. Zwrot niemożliwy.");

        // Zaktualizuj stan sprzętu przy zwrocie
        foreach (var re in rental.RentalEquipments)
        {
            if (equipmentConditions.TryGetValue(re.EquipmentId, out var condition))
                re.ConditionAtReturn = condition;

            await _equipmentRepo.UpdateStatusAsync(re.EquipmentId, EquipmentStatus.Available, ct);
        }

        rental.Status           = RentalStatus.Completed;
        rental.ActualReturnTime = DateTime.UtcNow;

        // Finalizuj płatność jeśli preautoryzowana
        if (rental.IsPreauthorized && rental.PayUOrderId is not null)
        {
            var captureResult = await _paymentProvider.CapturePreauthorization(
                rental.PayUOrderId, rental.TotalAmount);
            rental.PaymentStatus = captureResult.IsSuccess ? PaymentStatus.Paid : rental.PaymentStatus;
        }

        await _rentalRepo.UpdateAsync(rental, ct);

        _logger.LogInformation("Return processed for rental {RentalId}", rental.Id);
        return ReturnResult.Success(rental.Id, rental.ActualReturnTime.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════

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

    public static ReturnResult Success(int id, DateTime t) => new() { IsSuccess = true, RentalId = id, ReturnTime = t };
    public static ReturnResult Failure(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
}

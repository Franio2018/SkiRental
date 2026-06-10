using Microsoft.Extensions.Options;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Options;
using SkiRental.Logic.Services;

namespace SkiRental.App.Services;

/// <summary>
/// In-memory state carrier dla przepływu rezerwacji.
/// Zarejestrowany jako Scoped (jeden na sesję Blazor).
/// Zastępuje przekazywanie danych przez query-string JSON.
/// </summary>
public sealed class BookingStateService
{
    private readonly SkiRentalOptions _opts;

    public BookingStateService(IOptions<SkiRentalOptions> options)
        => _opts = options.Value;

    // ── BookingWizard → RecommendationLoading ─────────────────────────────────
    private EquipmentSelectionParameters? _selectionParameters;

    public void SetSelectionParameters(EquipmentSelectionParameters parameters)
        => _selectionParameters = parameters;

    public EquipmentSelectionParameters? GetSelectionParameters()
        => _selectionParameters;

    public void ClearSelectionParameters()
        => _selectionParameters = null;

    // ── Termin wypożyczenia (kalendarz → kreator / gear-review) ───────────────
    private DateTime? _rentalStart;
    private DateTime? _rentalEnd;

    public void SetRentalWindow(DateTime start, DateTime end)
    {
        _rentalStart = start;
        _rentalEnd = end;
    }

    public (DateTime Start, DateTime End) GetRentalWindowOrDefault()
    {
        if (_rentalStart.HasValue && _rentalEnd.HasValue)
            return (_rentalStart.Value, _rentalEnd.Value);

        var (startDate, endDate) = RentalWindowHelper.GetDefaultDates(_opts);
        return RentalWindowHelper.ToBookingWindow(startDate, endDate);
    }

    public void ClearRentalWindow()
    {
        _rentalStart = null;
        _rentalEnd = null;
    }

    // ── GearReview → Payment ─────────────────────────────────────────────────
    private GearSelection? _pendingGearSelection;

    public void SetGearSelection(GearSelection selection)
        => _pendingGearSelection = selection;

    public GearSelection? GetGearSelection()
        => _pendingGearSelection;

    public void ClearGearSelection()
        => _pendingGearSelection = null;

    // ── Payment → Confirmation ───────────────────────────────────────────────
    private ConfirmationData? _pendingConfirmation;

    public void SetConfirmation(ConfirmationData data)
        => _pendingConfirmation = data;

    public ConfirmationData? GetConfirmation()
        => _pendingConfirmation;

    public void ClearConfirmation()
        => _pendingConfirmation = null;

    // ── Recommendation → NoMatch ─────────────────────────────────────────────
    private RecommendationResult? _noMatchResult;

    public void SetNoMatchState(RecommendationResult result)
        => _noMatchResult = result;

    public RecommendationResult? GetNoMatchState()
        => _noMatchResult;

    public void ClearNoMatchState()
        => _noMatchResult = null;

    // ── Recommendation → GearReview ───────────────────────────────────────────
    private RecommendationResult? _recommendationResult;

    public void SetRecommendationResult(RecommendationResult result)
        => _recommendationResult = result;

    public RecommendationResult? GetRecommendationResult()
        => _recommendationResult;

    public void ClearRecommendationResult()
        => _recommendationResult = null;
}

/// <summary>
/// DTO przekazywane z PaymentPage do ConfirmationPage.
/// Silnie typowany zamiennik przekazywania dynamic przez query-string.
/// </summary>
public sealed class ConfirmationData
{
    public int           RentalId      { get; init; }
    public string        QrCode        { get; init; } = string.Empty;
    public decimal       TotalAmount   { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public DateTime      CreatedAt     { get; init; } = DateTime.Now;
}

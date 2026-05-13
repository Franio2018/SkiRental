using SkiRental.Domain.Enums;

namespace SkiRental.Domain.Models;

/// <summary>
/// Enhanced recommendation result with per-category match status and fallback handling.
/// Replaces the basic EquipmentRecommendation for the new rental flow.
/// </summary>
public record RecommendationResult
{
    /// <summary>Overall recommendation status.</summary>
    public RecommendationStatus Status { get; init; }
    
    /// <summary>Selected equipment per category (mutable for swaps).</summary>
    public Dictionary<EquipmentCategory, Equipment> Selection { get; init; } = new();
    
    /// <summary>Match quality per category.</summary>
    public Dictionary<EquipmentCategory, MatchStatus> MatchStatus { get; init; } = new();
    
    /// <summary>Fallback explanations when ideal size unavailable.</summary>
    public Dictionary<EquipmentCategory, string> FallbackNotes { get; init; } = new();
    
    /// <summary>Reasons why no match was found (when Status = NoMatch).</summary>
    public List<NoMatchReason> NoMatchReasons { get; init; } = new();
    
    /// <summary>Human-readable algorithm explanation.</summary>
    public string AlgorithmExplanation { get; init; } = string.Empty;
    
    /// <summary>Total daily rate for selected equipment.</summary>
    public decimal TotalDailyRate { get; init; }
    
    /// <summary>Original parameters used for calculation.</summary>
    public EquipmentSelectionParameters Parameters { get; init; } = null!;
}

/// <summary>
/// Overall recommendation outcome.
/// </summary>
public enum RecommendationStatus
{
    /// <summary>All categories have exact matches within ideal tolerance.</summary>
    Full,
    
    /// <summary>Some categories use fallback matches outside ideal tolerance.</summary>
    Partial,
    
    /// <summary>No safe match available for one or more required categories.</summary>
    NoMatch
}

/// <summary>
/// Match quality for a specific equipment category.
/// </summary>
public enum MatchStatus
{
    /// <summary>Exact match within ideal tolerance band.</summary>
    ExactMatch,
    
    /// <summary>Fallback match within safety tolerance but outside ideal band.</summary>
    FallbackMatch,
    
    /// <summary>No match available within safety tolerance.</summary>
    NoMatch
}

/// <summary>
/// Details about why no match was found for a category.
/// </summary>
public record NoMatchReason
{
    public EquipmentCategory Category { get; init; }
    public string ConstraintDescription { get; init; } = string.Empty;
    public decimal IdealSize { get; init; }
    public decimal MinAvailable { get; init; }
    public decimal MaxAvailable { get; init; }
    public string AvailableSizes { get; init; } = string.Empty;
    public List<GearPosition> AvailablePositions { get; init; } = new();
    public List<string> RecommendedSizes { get; init; } = new();
    public string FitNotes { get; init; } = string.Empty;
}

/// <summary>
/// Swap option for customer-facing gear selection.
/// </summary>
public record SwapOption
{
    public Equipment Equipment { get; init; } = null!;
    
    /// <summary>Difference from ideal size (positive = larger, negative = smaller).</summary>
    public decimal SizeDelta { get; init; }
    
    /// <summary>Fit score 0.0-1.0 (higher = better fit).</summary>
    public float FitScore { get; init; }
    
    /// <summary>Whether this is within the ideal tolerance band.</summary>
    public bool IsIdealSize { get; init; }
    
    /// <summary>Human-readable size description.</summary>
    public string SizeDescription { get; init; } = string.Empty;
}

/// <summary>
/// Gear selection passed from GearReviewPage to PaymentPage.
/// </summary>
public record GearSelection
{
    public int CustomerProfileId { get; init; }
    
    /// <summary>Equipment IDs per category.</summary>
    public Dictionary<EquipmentCategory, int> EquipmentIds { get; init; } = new();
    
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    
    /// <summary>Total rental amount calculated from selected equipment.</summary>
    public decimal TotalAmount { get; init; }
    
    /// <summary>Original recommendation result for reference.</summary>
    public RecommendationResult Recommendation { get; init; } = null!;
}

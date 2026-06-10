using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Enhanced recommendation service that orchestrates per-category equipment selection,
/// fallback logic, and no-match detection based on the specification.
/// </summary>
public sealed class RecommendationOrchestrator : IRecommendationOrchestrator
{
    private readonly EquipmentMathService _equipmentMath;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly ILogger<RecommendationOrchestrator> _logger;

    // Tolerance windows from specification
    private static readonly Dictionary<EquipmentCategory, (decimal IdealDelta, decimal MaxDelta)> Tolerances = new()
    {
        [EquipmentCategory.Skis] = (5m, 10m),   // Ideal ±5cm, Max ±10cm
        [EquipmentCategory.Poles] = (0m, 5m),    // No ideal band, Max ±5cm
        [EquipmentCategory.Boots] = (0m, 1m),    // Exact match only, Max ±1EU
    };

    public RecommendationOrchestrator(
        EquipmentMathService equipmentMath,
        IEquipmentRepository equipmentRepo,
        ILogger<RecommendationOrchestrator> logger)
    {
        _equipmentMath = equipmentMath;
        _equipmentRepo = equipmentRepo;
        _logger = logger;
    }

    public async Task<RecommendationResult> RecommendAsync(
        EquipmentSelectionParameters parameters,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting recommendation orchestration: H={Height}cm W={Weight}kg Style={Style} Skill={Skill}",
            parameters.Height, parameters.Weight, parameters.Style, parameters.SkillLevel);

        // Step 1: Compute ideal targets using existing EquipmentMathService
        var idealSkiLength = _equipmentMath.CalculateSkiLength(parameters);
        var idealPoleLength = _equipmentMath.CalculatePoleLengthCm(parameters);
        var idealStiffness = _equipmentMath.CalculateStiffness(parameters);
        var idealBootSize = parameters.ShoeSize;

        var explanation = BuildAlgorithmExplanation(parameters, idealSkiLength, idealPoleLength, idealStiffness);

        // Step 2: Define ideal targets per category
        var idealTargets = new Dictionary<EquipmentCategory, decimal>
        {
            [EquipmentCategory.Skis] = idealSkiLength,
            [EquipmentCategory.Poles] = idealPoleLength,
            [EquipmentCategory.Boots] = idealBootSize
        };

        // Step 3: Query available inventory per category
        var requiredCategories = new[] { EquipmentCategory.Skis, EquipmentCategory.Poles, EquipmentCategory.Boots };
        var availableByCategory = new Dictionary<EquipmentCategory, List<Equipment>>();

        foreach (var category in requiredCategories)
        {
            var available = await _equipmentRepo.GetAvailableByStyleAsync(parameters.Style, category, ct);
            availableByCategory[category] = available.ToList();
        }

        // Step 4: Score and select best match per category
        var selection = new Dictionary<EquipmentCategory, Equipment>();
        var matchStatus = new Dictionary<EquipmentCategory, MatchStatus>();
        var fallbackNotes = new Dictionary<EquipmentCategory, string>();
        var noMatchReasons = new List<NoMatchReason>();

        foreach (var category in requiredCategories)
        {
            var available = availableByCategory[category];
            var idealTarget = idealTargets[category];
            var tolerance = Tolerances[category];

            var candidates = available
                .Where(e => Math.Abs(e.Size - idealTarget) <= tolerance.MaxDelta)
                .ToList()
                .OrderBy(e => Math.Abs(e.Size - idealTarget))
                .ThenBy(e => category == EquipmentCategory.Skis ? Math.Abs((e.Stiffness ?? 0) - idealStiffness) : 0)
                .ThenBy(e => e.TotalRentalDays);

            var bestMatch = candidates.FirstOrDefault();

            if (bestMatch == null)
            {
                // No match within safety tolerance
                matchStatus[category] = MatchStatus.NoMatch;
                noMatchReasons.Add(BuildNoMatchReason(category, idealTarget, tolerance, available));
            }
            else if (Math.Abs(bestMatch.Size - idealTarget) > tolerance.IdealDelta)
            {
                // Fallback match - within safety but outside ideal band
                matchStatus[category] = MatchStatus.FallbackMatch;
                selection[category] = bestMatch;
                fallbackNotes[category] = BuildFallbackNote(bestMatch, idealTarget, category);
            }
            else
            {
                // Exact match within ideal tolerance
                matchStatus[category] = MatchStatus.ExactMatch;
                selection[category] = bestMatch;
            }
        }

        // Step 5: Determine overall status
        var overallStatus = noMatchReasons.Any() 
            ? RecommendationStatus.NoMatch 
            : matchStatus.Values.Any(ms => ms == MatchStatus.FallbackMatch) 
                ? RecommendationStatus.Partial 
                : RecommendationStatus.Full;

        // Step 6: Halt if any category has no safe match
        if (overallStatus == RecommendationStatus.NoMatch)
        {
            return new RecommendationResult
            {
                Status = RecommendationStatus.NoMatch,
                NoMatchReasons = noMatchReasons,
                AlgorithmExplanation = explanation,
                Parameters = parameters,
                TotalDailyRate = 0
            };
        }

        // Step 7: Return successful recommendation
        var totalDailyRate = selection.Values.Sum(e => e.DailyRate);

        return new RecommendationResult
        {
            Status = overallStatus,
            Selection = selection,
            MatchStatus = matchStatus,
            FallbackNotes = fallbackNotes,
            AlgorithmExplanation = explanation,
            Parameters = parameters,
            TotalDailyRate = totalDailyRate
        };
    }

    private static NoMatchReason BuildNoMatchReason(
        EquipmentCategory category,
        decimal idealTarget,
        (decimal IdealDelta, decimal MaxDelta) tolerance,
        List<Equipment> available)
    {
        var availableSizes = available.Any() 
            ? string.Join(", ", available.Select(e => $"{e.Size}cm").OrderBy(s => s))
            : "None available";

        var minAvailable = available.Any() ? available.Min(e => e.Size) : 0;
        var maxAvailable = available.Any() ? available.Max(e => e.Size) : 0;

        var constraintDescription = category switch
        {
            EquipmentCategory.Skis => $"No skis in the {idealTarget - tolerance.MaxDelta:F0}–{idealTarget + tolerance.MaxDelta:F0}cm range available",
            EquipmentCategory.Poles => $"No poles in the {idealTarget - tolerance.MaxDelta:F0}–{idealTarget + tolerance.MaxDelta:F0}cm range available",
            EquipmentCategory.Boots => $"No boots in size {idealTarget - tolerance.MaxDelta:F0}–{idealTarget + tolerance.MaxDelta:F0} EU available",
            _ => $"No {category.ToString().ToLower()} available within safe range"
        };

        return new NoMatchReason
        {
            Category = category,
            ConstraintDescription = constraintDescription,
            IdealSize = idealTarget,
            MinAvailable = minAvailable,
            MaxAvailable = maxAvailable,
            AvailableSizes = availableSizes
        };
    }

    private static string BuildFallbackNote(
        Equipment equipment,
        decimal idealTarget,
        EquipmentCategory category)
    {
        var delta = equipment.Size - idealTarget;
        var direction = delta > 0 ? "longer" : "shorter";

        return category switch
        {
            EquipmentCategory.Skis => $"Ideal ski length: {idealTarget:F0}cm. Selected {equipment.Size:F0}cm is {Math.Abs(delta):F0}cm {direction} (within safety tolerance).",
            EquipmentCategory.Poles => $"Ideal pole length: {idealTarget:F0}cm. Selected {equipment.Size:F0}cm is {Math.Abs(delta):F0}cm {direction}.",
            EquipmentCategory.Boots => $"Ideal boot size: {idealTarget:F0} EU. Selected {equipment.Size:F0} EU is {Math.Abs(delta):F0} size different.",
            _ => $"Size adjusted from ideal {idealTarget:F0} to {equipment.Size:F0} for safety."
        };
    }

    private static string BuildAlgorithmExplanation(
        EquipmentSelectionParameters parameters,
        decimal skiLength,
        decimal poleLength,
        int stiffness)
    {
        var style = parameters.Style == SkiStyle.Classic ? "Classic" : "Skate";
        var styleOffset = parameters.Style == SkiStyle.Classic ? "~25" : "~12";

        return $"Style {style} | Height {parameters.Height} cm | Weight {parameters.Weight} kg | BMI {parameters.Bmi}\n" +
               $"Skis: {parameters.Height} + {styleOffset} cm" +
               (parameters.IsBmiNormal ? "" : " + BMI correction") + $" = {skiLength:F0} cm\n" +
               $"Poles: {parameters.Height} × {(parameters.Style == SkiStyle.Classic ? "0.85" : "0.90")} = {poleLength:F0} cm\n" +
               $"Flex: {stiffness} (level {parameters.SkillLevel})";
    }
}

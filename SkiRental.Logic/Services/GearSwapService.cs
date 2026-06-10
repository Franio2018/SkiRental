using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Service for providing customer-facing gear swap options.
/// Returns scored and filtered alternatives within safety tolerance.
/// </summary>
public sealed class GearSwapService : IGearSwapService
{
    private readonly EquipmentMathService _equipmentMath;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly ILogger<GearSwapService> _logger;

    // Maximum number of swap options to show (UX constraint from specification)
    private const int MaxSwapOptions = 6;

    // Safety tolerances (same as RecommendationOrchestrator)
    private static readonly Dictionary<EquipmentCategory, decimal> SafetyTolerances = new()
    {
        [EquipmentCategory.Skis] = 10m,   // ±10 cm
        [EquipmentCategory.Poles] = 5m,    // ±5 cm
        [EquipmentCategory.Boots] = 1m,    // ±1 EU
    };

    public GearSwapService(
        EquipmentMathService equipmentMath,
        IEquipmentRepository equipmentRepo,
        ILogger<GearSwapService> logger)
    {
        _equipmentMath = equipmentMath;
        _equipmentRepo = equipmentRepo;
        _logger = logger;
    }

    public async Task<List<SwapOption>> GetSwapOptionsAsync(
        EquipmentCategory category,
        EquipmentSelectionParameters parameters,
        int excludeEquipmentId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting swap options for {Category} - excluding equipment {ExcludeId}",
            category, excludeEquipmentId);

        // Get ideal target size for this category
        var idealTarget = GetIdealTargetSize(category, parameters);
        var tolerance = SafetyTolerances[category];

        // Query available equipment for this category and style
        var available = await _equipmentRepo.GetAvailableByStyleAsync(parameters.Style, category, ct);

        // Filter and score options
        var swapOptions = available
            .Where(e => e.Id != excludeEquipmentId)  // Exclude current selection
            .Where(e => Math.Abs(e.Size - idealTarget) <= tolerance)  // Within safety tolerance
            .Select(e => CreateSwapOption(e, idealTarget, parameters))
            .ToList()
            .OrderByDescending(o => o.FitScore)  // Best fit first
            .ThenBy(o => Math.Abs(o.SizeDelta))  // Then closest to ideal
            .Take(MaxSwapOptions)
            .ToList();

        _logger.LogInformation(
            "Returning {Count} swap options for {Category}",
            swapOptions.Count, category);

        return swapOptions;
    }

    private static decimal GetIdealTargetSize(EquipmentCategory category, EquipmentSelectionParameters parameters)
    {
        // This would typically use EquipmentMathService, but for simplicity we'll calculate directly
        return category switch
        {
            EquipmentCategory.Skis => parameters.Height + (parameters.Style == SkiStyle.Classic ? 25m : 12.5m),
            EquipmentCategory.Poles => parameters.Height * (parameters.Style == SkiStyle.Classic ? 0.85m : 0.90m),
            EquipmentCategory.Boots => parameters.ShoeSize,
            _ => 0
        };
    }

    private static SwapOption CreateSwapOption(
        Equipment equipment,
        decimal idealTarget,
        EquipmentSelectionParameters parameters)
    {
        var sizeDelta = equipment.Size - idealTarget;
        var fitScore = ComputeFitScore(equipment, idealTarget, parameters);
        var isIdealSize = Math.Abs(sizeDelta) <= GetIdealTolerance(equipment.Category);

        var sizeDescription = BuildSizeDescription(sizeDelta, equipment.Category);

        return new SwapOption
        {
            Equipment = equipment,
            SizeDelta = sizeDelta,
            FitScore = fitScore,
            IsIdealSize = isIdealSize,
            SizeDescription = sizeDescription
        };
    }

    private static float ComputeFitScore(
        Equipment equipment,
        decimal idealTarget,
        EquipmentSelectionParameters parameters)
    {
        // Base score on size proximity (0.0 to 1.0)
        var tolerance = SafetyTolerances[equipment.Category];
        var sizeProximity = 1.0f - (float)(Math.Abs(equipment.Size - idealTarget) / tolerance);

        // For skis, also consider stiffness match
        if (equipment.Category == EquipmentCategory.Skis && equipment.Stiffness.HasValue)
        {
            var idealStiffness = GetIdealStiffness(parameters);
            var stiffnessDiff = Math.Abs(equipment.Stiffness.Value - idealStiffness);
            var stiffnessScore = Math.Max(0, 1.0f - (stiffnessDiff / 30.0f)); // 30 is max reasonable stiffness range
            
            // Weight size more heavily than stiffness (70% size, 30% stiffness)
            return (sizeProximity * 0.7f) + (stiffnessScore * 0.3f);
        }

        return sizeProximity;
    }

    private static int GetIdealStiffness(EquipmentSelectionParameters parameters)
    {
        return parameters.SkillLevel switch
        {
            SkillLevel.Beginner => 42,
            SkillLevel.Intermediate => 52,
            SkillLevel.Advanced => 62,
            SkillLevel.Expert => 72,
            _ => 52
        };
    }

    private static decimal GetIdealTolerance(EquipmentCategory category)
    {
        return category switch
        {
            EquipmentCategory.Skis => 5m,   // Ideal ±5cm
            EquipmentCategory.Poles => 0m,   // No ideal band
            EquipmentCategory.Boots => 0m,   // Exact match only
            _ => 0m
        };
    }

    private static string BuildSizeDescription(decimal sizeDelta, EquipmentCategory category)
    {
        if (Math.Abs(sizeDelta) < 0.1m)
            return "Perfect fit";

        var direction = sizeDelta > 0 ? "larger" : "smaller";
        var unit = category == EquipmentCategory.Boots ? "size" : "cm";

        return $"{Math.Abs(sizeDelta):F1} {unit} {direction}";
    }
}

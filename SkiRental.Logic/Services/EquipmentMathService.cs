using Microsoft.Extensions.Logging;
using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Serwis "Inteligentny Asystent" – algorytm doboru sprzętu narciarskiego.
///
/// Wzory (wg norm FIS + empiryczne korekty):
///   Classic:  Narty = wzrost + 20..30 cm;  Kijki = wzrost × 0.85
///   Skate:    Narty = wzrost + 10..15 cm;  Kijki = wzrost × 0.90
///   Korekta BMI: ±5 cm do długości przy wadze poza normą 18.5–24.9
/// </summary>
public sealed class EquipmentMathService
{
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly ILogger<EquipmentMathService> _logger;

    // ── Stałe algorytmu ──────────────────────────────────────────────────────

    private const decimal ClassicSkiOffsetMin = 20m;
    private const decimal ClassicSkiOffsetMax = 30m;
    private const decimal ClassicPoleMultiplier = 0.85m;

    private const decimal SkateSkiOffsetMin = 10m;
    private const decimal SkateSkiOffsetMax = 15m;
    private const decimal SkatePoleMultiplier = 0.90m;

    private const decimal BmiCorrectionCm = 5m;

    private const decimal BmiNormalMin = 18.5m;
    private const decimal BmiNormalMax = 24.9m;

    // ── Stiffness (Flex) bazowe wg poziomu – indeks FIS ──────────────────────
    private static readonly Dictionary<SkillLevel, int> BaseStiffness = new()
    {
        [SkillLevel.Beginner]     = 42,
        [SkillLevel.Intermediate] = 52,
        [SkillLevel.Advanced]     = 62,
        [SkillLevel.Expert]       = 72
    };

    public EquipmentMathService(IEquipmentRepository equipmentRepo,
                                ILogger<EquipmentMathService> logger)
    {
        _equipmentRepo = equipmentRepo;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Publiczne API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Główna metoda – zwraca pełną rekomendację sprzętu wraz z listą
    /// dostępnych egzemplarzy z bazy.
    /// </summary>
    public async Task<EquipmentRecommendation> RecommendAsync(
        EquipmentSelectionParameters p,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Calculating recommendation: H={Height}cm W={Weight}kg Style={Style} Skill={Skill}",
            p.Height, p.Weight, p.Style, p.SkillLevel);

        var skiLength  = CalculateSkiLength(p);
        var poleLength = CalculatePoleLengthCm(p);
        var stiffness  = CalculateStiffness(p);
        var (corrApplied, corrNote) = GetBmiCorrectionInfo(p);

        // Pobierz dostępne narty i kijki z repozytorium
        var availableSkis = await _equipmentRepo.GetAvailableByStyleAsync(
            p.Style, EquipmentCategory.Skis, ct);

        var availablePoles = await _equipmentRepo.GetAvailableByStyleAsync(
            p.Style, EquipmentCategory.Poles, ct);

        var availableBoots = await _equipmentRepo.GetAvailableByStyleAsync(
            p.Style, EquipmentCategory.Boots, ct);

        // Filtruj po rozmiarze z tolerancją ±5 cm
        const decimal tolerance = 5m;

        var matchingSkis = availableSkis
            .Where(e => Math.Abs(e.Size - skiLength) <= tolerance)
            .ToList()
            .OrderBy(e => Math.Abs(e.Size - skiLength));

        var matchingPoles = availablePoles
            .Where(e => Math.Abs(e.Size - poleLength) <= tolerance)
            .ToList()
            .OrderBy(e => Math.Abs(e.Size - poleLength));

        var matchingBoots = availableBoots
            .Where(e => Math.Abs(e.Size - p.ShoeSize) <= 1m)
            .OrderBy(e => Math.Abs(e.Size - p.ShoeSize))
            .ToList();

        var allMatching = matchingSkis.Concat(matchingPoles).Concat(matchingBoots).ToList();

        return new EquipmentRecommendation
        {
            RecommendedSkiLength   = skiLength,
            RecommendedPoleLength  = poleLength,
            RecommendedStiffness   = stiffness,
            RecommendedBootSize    = p.ShoeSize,
            Style                  = p.Style,
            SkillLevel             = p.SkillLevel,
            BmiCorrectionApplied   = corrApplied,
            BmiCorrectionNote      = corrNote,
            AlgorithmExplanation   = BuildExplanation(p, skiLength, poleLength, stiffness, corrApplied),
            MatchingEquipment      = allMatching.AsReadOnly()
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Metody obliczeniowe (wewnętrzne, dostępne do testów jednostkowych)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Oblicza zalecaną długość narty [cm].
    /// Classic:  wzrost + 20..30 cm (środek przedziału = +25, beginner → +20, expert → +30)
    /// Skate:    wzrost + 10..15 cm (środek = +12.5)
    /// Korekta BMI: –5 cm gdy niedowaga, +5 cm gdy nadwaga.
    /// </summary>
    public decimal CalculateSkiLength(EquipmentSelectionParameters p)
    {
        decimal baseOffset = p.Style == SkiStyle.Classic
            ? InterpolateOffset(ClassicSkiOffsetMin, ClassicSkiOffsetMax, p.SkillLevel)
            : InterpolateOffset(SkateSkiOffsetMin,   SkateSkiOffsetMax,   p.SkillLevel);

        decimal length = p.Height + baseOffset + GetBmiLengthCorrection(p);

        // Zaokrąglij do 2,5 cm (standardowe skoki produkcyjne)
        return RoundToNearestHalf(length, 2.5m);
    }

    /// <summary>
    /// Oblicza zalecaną długość kijków [cm].
    /// Classic:  wzrost × 0.85
    /// Skate:    wzrost × 0.90
    /// </summary>
    public decimal CalculatePoleLengthCm(EquipmentSelectionParameters p)
    {
        decimal multiplier = p.Style == SkiStyle.Classic
            ? ClassicPoleMultiplier
            : SkatePoleMultiplier;

        decimal raw = p.Height * multiplier;
        return RoundToNearestHalf(raw, 2.5m);
    }

    /// <summary>
    /// Oblicza zalecany indeks sztywności narty.
    /// Bazowy wg poziomu + korekta BMI (±5 punktów Flex).
    /// </summary>
    public int CalculateStiffness(EquipmentSelectionParameters p)
    {
        int baseStiff = BaseStiffness[p.SkillLevel];

        // Dodaj korektę wagową – cięższa osoba potrzebuje twardszej narty
        int bmiCorrection = 0;
        if (p.IsOverweight) bmiCorrection = +5;
        else if (p.IsUnderweight) bmiCorrection = -5;

        return Math.Clamp(baseStiff + bmiCorrection, 30, 90);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers prywatne
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Interpoluje offset długości narty względem poziomu zaawansowania.
    /// Beginner → min,  Expert → max.
    /// </summary>
    private static decimal InterpolateOffset(decimal min, decimal max, SkillLevel level)
    {
        var t = (decimal)level / (decimal)(SkillLevel.Expert);
        return Math.Round(min + t * (max - min), 1);
    }

    private static decimal GetBmiLengthCorrection(EquipmentSelectionParameters p)
    {
        if (p.Bmi < BmiNormalMin) return -BmiCorrectionCm; // niedowaga → krótsza narta
        if (p.Bmi > BmiNormalMax) return +BmiCorrectionCm; // nadwaga   → dłuższa narta
        return 0m;
    }

    private static (bool applied, string? note) GetBmiCorrectionInfo(EquipmentSelectionParameters p)
    {
        if (p.Bmi < BmiNormalMin)
            return (true, $"BMI={p.Bmi} (niedowaga) – skrócono nartę o {BmiCorrectionCm} cm");
        if (p.Bmi > BmiNormalMax)
            return (true, $"BMI={p.Bmi} (nadwaga) – wydłużono nartę o {BmiCorrectionCm} cm");
        return (false, null);
    }

    /// <summary>Zaokrąglenie do najbliższego wielokrotności step.</summary>
    private static decimal RoundToNearestHalf(decimal value, decimal step) =>
        Math.Round(value / step) * step;

    private static string BuildExplanation(EquipmentSelectionParameters p,
        decimal skiLen, decimal poleLen, int stiffness, bool bmiCorr)
    {
        var style = p.Style == SkiStyle.Classic ? "Klasyczny" : "Łyżwowy";
        var mult  = p.Style == SkiStyle.Classic ? ClassicPoleMultiplier : SkatePoleMultiplier;

        return $"Styl {style} | Wzrost {p.Height} cm | Waga {p.Weight} kg | BMI {p.Bmi}\n" +
               $"Narty: {p.Height} + {(p.Style == SkiStyle.Classic ? "~25" : "~12")} cm" +
               (bmiCorr ? " + korekta BMI" : "") + $" = {skiLen} cm\n" +
               $"Kijki: {p.Height} × {mult} = {poleLen} cm\n" +
               $"Flex: {stiffness} (poziom {p.SkillLevel})";
    }
}

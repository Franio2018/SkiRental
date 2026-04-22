using SkiRental.Domain.Enums;

namespace SkiRental.Domain.Models;

/// <summary>
/// Wynik rekomendacji algorytmu "Inteligentny Asystent".
/// </summary>
public record EquipmentRecommendation
{
    /// <summary>Zalecana długość nart [cm].</summary>
    public decimal RecommendedSkiLength { get; init; }

    /// <summary>Zalecana długość kijków [cm].</summary>
    public decimal RecommendedPoleLength { get; init; }

    /// <summary>Zalecana sztywność narty (Flex Index).</summary>
    public int RecommendedStiffness { get; init; }

    /// <summary>Zalecany rozmiar buta (EU).</summary>
    public int RecommendedBootSize { get; init; }

    /// <summary>Wybrany styl jazdy.</summary>
    public SkiStyle Style { get; init; }

    /// <summary>Poziom zaawansowania.</summary>
    public SkillLevel SkillLevel { get; init; }

    /// <summary>Czy zastosowano korektę BMI.</summary>
    public bool BmiCorrectionApplied { get; init; }

    /// <summary>Opis korekty BMI (dla UI).</summary>
    public string? BmiCorrectionNote { get; init; }

    /// <summary>Wyjaśnienie algorytmu (dla UI).</summary>
    public string AlgorithmExplanation { get; init; } = string.Empty;

    /// <summary>Lista sprzętu pasującego do rekomendacji (po filtrze z DB).</summary>
    public IReadOnlyList<Equipment> MatchingEquipment { get; init; } = [];
}

/// <summary>
/// Parametry wejściowe dla algorytmu doboru sprzętu.
/// </summary>
public record EquipmentSelectionParameters
{
    public int Height { get; init; }
    public decimal Weight { get; init; }
    public int ShoeSize { get; init; }
    public SkiStyle Style { get; init; }
    public SkillLevel SkillLevel { get; init; }

    /// <summary>Wyliczony BMI.</summary>
    public decimal Bmi => Height > 0
        ? Math.Round(Weight / ((decimal)(Height / 100.0) * (decimal)(Height / 100.0)), 1)
        : 0;

    /// <summary>Czy waga jest powyżej normy BMI (> 24.9).</summary>
    public bool IsOverweight => Bmi > 24.9m;

    /// <summary>Czy waga jest poniżej normy BMI (< 18.5).</summary>
    public bool IsUnderweight => Bmi < 18.5m;

    /// <summary>Czy BMI mieści się w normie (18.5–24.9).</summary>
    public bool IsBmiNormal => Bmi is >= 18.5m and <= 24.9m;
}

/// <summary>
/// Wynik analizy użycia sprzętu dla back-office.
/// </summary>
public record UtilizationReport
{
    public int EquipmentId { get; init; }
    public string SerialNumber { get; init; } = string.Empty;
    public string EquipmentDescription { get; init; } = string.Empty;
    public EquipmentCategory Category { get; init; }

    /// <summary>Łączny czas wypożyczeń [godz].</summary>
    public double TotalRentedHours { get; init; }

    /// <summary>Całkowity dostępny czas sezonu [godz].</summary>
    public double SeasonAvailableHours { get; init; }

    /// <summary>Wskaźnik użycia [0.0 – 1.0].</summary>
    public double UtilizationRate => SeasonAvailableHours > 0
        ? Math.Round(TotalRentedHours / SeasonAvailableHours, 4)
        : 0;

    /// <summary>Wskaźnik użycia [%].</summary>
    public double UtilizationRatePercent => UtilizationRate * 100;

    /// <summary>Liczba wypożyczeń w sezonie.</summary>
    public int TotalRentals { get; init; }

    /// <summary>Przychód wygenerowany przez sprzęt [PLN].</summary>
    public decimal Revenue { get; init; }

    public DateTime SeasonStart { get; init; }
    public DateTime SeasonEnd { get; init; }
}

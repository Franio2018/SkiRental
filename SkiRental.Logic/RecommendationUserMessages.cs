using SkiRental.Domain.Enums;
using SkiRental.Domain.Models;

namespace SkiRental.Logic;

/// <summary>
/// Komunikaty dla klienta przy dopasowaniu sprzętu (PL).
/// </summary>
public static class RecommendationUserMessages
{
    public static readonly EquipmentCategory[] RequiredPackage = RentalEquipmentCategories.All;

    public static bool HasSkisNoMatch(IEnumerable<NoMatchReason> reasons) =>
        reasons.Any(r => r.Category == EquipmentCategory.Skis);

    public static string GetNoMatchTitle(IReadOnlyList<NoMatchReason> reasons) =>
        HasSkisNoMatch(reasons)
            ? "Nie udało się dobrać odpowiednich nart"
            : "Brak pełnego pakietu wypożyczenia";

    public static string GetNoMatchLead(IReadOnlyList<NoMatchReason> reasons)
    {
        if (HasSkisNoMatch(reasons))
        {
            return "W magazynie nie ma nart w bezpiecznym zakresie rozmiaru dla Twojego wzrostu, "
                 + "wagi i stylu jazdy. Bez nart nie możemy złożyć kompletnej rezerwacji "
                 + "(narty + kijki + buty).";
        }

        var missing = FormatMissingCategories(reasons);
        return $"Nie możemy zaproponować pełnego pakietu ({FormatRequiredPackage()}). "
             + $"Brakuje bezpiecznego dopasowania dla: {missing}.";
    }

    public static string GetPartialPackageWarning(RecommendationResult result)
    {
        var fallback = result.MatchStatus
            .Where(kv => kv.Value == MatchStatus.FallbackMatch)
            .Select(kv => CategoryNamePl(kv.Key))
            .ToList();

        if (fallback.Count == 0)
            return string.Empty;

        return "Część sprzętu jest poza idealnym zakresem, ale nadal w granicach bezpieczeństwa: "
             + string.Join(", ", fallback) + ".";
    }

    public static string CategoryNamePl(EquipmentCategory category) => category switch
    {
        EquipmentCategory.Skis => "narty",
        EquipmentCategory.Poles => "kijki",
        EquipmentCategory.Boots => "buty",
        _ => category.ToString().ToLowerInvariant()
    };

    public static string FormatRequiredPackage() =>
        string.Join(" + ", RequiredPackage.Select(CategoryNamePl));

    public static string FormatMissingCategories(IEnumerable<NoMatchReason> reasons) =>
        string.Join(", ", reasons.Select(r => CategoryNamePl(r.Category)));

    public static string SpotPaymentHint =>
        "Możesz zapłacić gotówką lub kartą przy odbiorze sprzętu w wypożyczalni — bez płatności online.";
}

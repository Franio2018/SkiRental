namespace SkiRental.Domain.Enums;

/// <summary>
/// Poziom zaawansowania narciarza.
/// </summary>
public enum SkillLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}

/// <summary>
/// Technika narciarstwa biegowego.
/// </summary>
public enum SkiStyle
{
    /// <summary>Styl klasyczny – narty prowadzone równolegle w śladach.</summary>
    Classic = 0,

    /// <summary>Styl łyżwowy (skate) – technika przypominająca łyżwiarstwo.</summary>
    Skate = 1,

    /// <summary>Kombinowany – zestaw uniwersalny (kombi).</summary>
    Combi = 2
}

/// <summary>
/// Stan sprzętu w wypożyczalni.
/// </summary>
public enum EquipmentStatus
{
    Available = 0,
    Reserved = 1,
    Rented = 2,
    Maintenance = 3,
    Retired = 4
}

/// <summary>
/// Kategoria sprzętu.
/// </summary>
public enum EquipmentCategory
{
    Skis = 0,
    Poles = 1,
    Boots = 2,
    Gloves = 3,
    Helmet = 4
}

/// <summary>
/// Status wypożyczenia.
/// </summary>
public enum RentalStatus
{
    Pending = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    Overdue = 4
}

/// <summary>
/// Status płatności.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 0,
    Preauthorized = 1,
    Paid = 2,
    Refunded = 3,
    Failed = 4
}

/// <summary>
/// Metoda płatności.
/// </summary>
public enum PaymentMethod
{
    Spot = 0,
    Online = 1
}

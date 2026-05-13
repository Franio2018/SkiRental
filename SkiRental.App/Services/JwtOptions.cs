namespace SkiRental.App.Services;

/// <summary>
/// Configuration options for JWT token generation and validation
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// The issuer of the JWT token (typically your application URL)
    /// </summary>
    public string Issuer { get; set; } = "https://skirent.app";

    /// <summary>
    /// The audience of the JWT token (typically your client application)
    /// </summary>
    public string Audience { get; set; } = "SkiRental.App";

    /// <summary>
    /// The secret key used for signing the JWT token (should be a long, random string)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes (default: 60 minutes)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Clock skew in minutes to account for time differences between servers (default: 5 minutes)
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}

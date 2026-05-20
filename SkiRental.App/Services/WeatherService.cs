using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SkiRental.App.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public record SkiWeatherReport
{
    public string Location      { get; init; } = string.Empty;
    public double TemperatureCelsius { get; init; }
    public double WindSpeedMs   { get; init; }
    public string Description   { get; init; } = string.Empty;
    public string Icon          { get; init; } = string.Empty;
    public double SnowDepthMm   { get; init; }
    public bool   IsGoodForSkiing { get; init; }
    public string SkiConditionLabel { get; init; } = string.Empty;
    public DateTime FetchedAt   { get; init; } = DateTime.UtcNow;
}

// ═══════════════════════════════════════════════════════════════════════════════
// WeatherService
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Pobiera dane pogodowe z OpenWeather API (One Call 3.0 lub Current Weather).
/// Wyświetlane w aplikacji jako "Warunki narciarskie dziś".
/// </summary>
public sealed class WeatherService
{
    private readonly HttpClient _http;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _apiKey;
    private readonly double _defaultLat;
    private readonly double _defaultLon;
    private readonly int _cacheMinutes;

    // Cache: odświeżaj nie częściej niż co X minut
    private SkiWeatherReport? _cached;
    private DateTime _cacheExpiry = DateTime.MinValue;

    public WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> logger)
    {
        _http         = http;
        _logger       = logger;
        _apiKey       = config["OpenWeather:ApiKey"];
        // If API key is missing or placeholder, log warning and use fallback mode
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_OPENWEATHER_API_KEY")
        {
            _logger.LogWarning("OpenWeather API key not configured. Weather service will return fallback reports.");
            _apiKey = null;
        }
        _defaultLat   = double.Parse(config["OpenWeather:DefaultLatitude"]  ?? "49.2992", CultureInfo.InvariantCulture);
        _defaultLon   = double.Parse(config["OpenWeather:DefaultLongitude"] ?? "19.9496", CultureInfo.InvariantCulture);
        _cacheMinutes = int.Parse(config["OpenWeather:CacheMinutes"] ?? "10", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Pobiera raport warunków narciarskich dla podanej lokalizacji.
    /// </summary>
    /// <param name="lat">Szerokość geograficzna stacji narciarskiej.</param>
    /// <param name="lon">Długość geograficzna stacji narciarskiej.</param>
    public async Task<SkiWeatherReport> GetSkiConditionsAsync(
        double? lat = null,
        double? lon = null,
        CancellationToken ct = default)
    {
        // Use configured defaults if not provided
        var actualLat = lat ?? _defaultLat;
        var actualLon = lon ?? _defaultLon;

        if (_cached is not null && DateTime.UtcNow < _cacheExpiry)
        {
            _logger.LogDebug("Returning cached weather report");
            return _cached;
        }

        try
        {
            var url = $"data/2.5/weather?lat={actualLat}&lon={actualLon}&appid={_apiKey}&units=metric&lang=pl";
            var json = await _http.GetFromJsonAsync<JsonDocument>(url, ct);

            if (json is null) return FallbackReport();

            var root = json.RootElement;
            var temp = root.GetProperty("main").GetProperty("temp").GetDouble();
            var wind = root.GetProperty("wind").GetProperty("speed").GetDouble();
            var desc = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";
            var icon = root.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";
            var name = root.GetProperty("name").GetString() ?? "Nieznana lokalizacja";

            // Opady śniegu (w mm/3h) – jeśli dostępne
            double snowMm = 0;
            if (root.TryGetProperty("snow", out var snowEl) &&
                snowEl.TryGetProperty("1h", out var snow1h))
                snowMm = snow1h.GetDouble();

            var report = new SkiWeatherReport
            {
                Location           = name,
                TemperatureCelsius = temp,
                WindSpeedMs        = wind,
                Description        = char.ToUpper(desc[0]) + desc[1..],
                Icon               = $"https://openweathermap.org/img/wn/{icon}@2x.png",
                SnowDepthMm        = snowMm,
                IsGoodForSkiing    = EvaluateSkiConditions(temp, wind, snowMm),
                SkiConditionLabel  = BuildSkiLabel(temp, wind, snowMm)
            };

            _cached     = report;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(_cacheMinutes);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data");
            return FallbackReport();
        }
    }

    // ── Logika oceny warunków narciarskich ───────────────────────────────────

    private static bool EvaluateSkiConditions(double temp, double wind, double snow) =>
        temp is >= -20 and <= 0 &&   // idealna temperatura
        wind < 15 &&                  // wiatr < 15 m/s
        snow >= 0;                    // nie ma znaczenia – śnieg leży na trasach

    private static string BuildSkiLabel(double temp, double wind, double snow)
    {
        if (temp > 2)  return "❌ Za ciepło – możliwe topnienie";
        if (temp < -20) return "🥶 Ekstremalny mróz – ostrożnie";
        if (wind > 15)  return "💨 Silny wiatr – utrudnione warunki";
        if (snow > 5)   return "❄️ Świeży śnieg – doskonałe warunki!";
        if (temp <= 0)  return "✅ Dobre warunki narciarskie";
        return "⚠️ Warunki zmienne";
    }

    private static SkiWeatherReport FallbackReport() => new()
    {
        Location           = "Brak połączenia",
        Description        = "Sprawdź warunki bezpośrednio na stacji",
        IsGoodForSkiing    = false,
        SkiConditionLabel  = "📡 Brak danych pogodowych"
    };
}

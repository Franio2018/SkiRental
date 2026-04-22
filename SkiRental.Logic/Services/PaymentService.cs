using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// Konfiguracja PayU
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class PayUOptions
{
    public const string SectionName = "PayU";

    /// <summary>ID punktu płatności (pos_id) z panelu PayU.</summary>
    public string PosId { get; set; } = string.Empty;

    /// <summary>Klucz MD5 do podpisywania zapytań.</summary>
    public string SignatureKey { get; set; } = string.Empty;

    /// <summary>Client ID do OAuth2.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client Secret do OAuth2.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>https://secure.snd.payu.com (sandbox) lub https://secure.payu.com (prod).</summary>
    public string BaseUrl { get; set; } = "https://secure.snd.payu.com";

    /// <summary>URL do którego PayU wyśle notyfikację IPN.</summary>
    public string NotifyUrl { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════════════════════
// PayUPaymentProvider
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Implementacja IPaymentProvider dla systemu PayU z obsługą BLIK.
/// Dokumentacja: https://developers.payu.com/en/blik.html
/// </summary>
public sealed class PayUPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly PayUOptions _options;
    private readonly ILogger<PayUPaymentProvider> _logger;

    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public PayUPaymentProvider(
        HttpClient httpClient,
        IOptions<PayUOptions> options,
        ILogger<PayUPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _options    = options.Value;
        _logger     = logger;
    }

    // ── 1. BLIK ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<PaymentResult> InitiateBlikPayment(
        decimal amount, string blikCode, string orderId, string description)
    {
        _logger.LogInformation("Initiating BLIK payment. OrderId={OrderId} Amount={Amount}", orderId, amount);

        try
        {
            var token  = await GetAccessTokenAsync();
            var amountGr = (int)(amount * 100); // PayU przyjmuje grosze

            var payload = new
            {
                notifyUrl      = _options.NotifyUrl,
                customerIp     = "127.0.0.1",
                merchantPosId  = _options.PosId,
                description,
                currencyCode   = "PLN",
                totalAmount    = amountGr.ToString(),
                extOrderId     = orderId,
                payMethods = new
                {
                    payMethod = new
                    {
                        type         = "PBL",
                        value        = "BLIK",
                        authorizationCode = blikCode
                    }
                },
                products = new[]
                {
                    new { name = description, unitPrice = amountGr.ToString(), quantity = "1" }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_options.BaseUrl}/api/v2_1/orders")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("PayU BLIK response: {Body}", body);

            if (!response.IsSuccessStatusCode)
                return PaymentResult.Failure("PAYU_ERROR", $"HTTP {(int)response.StatusCode}: {body}");

            var json = JsonDocument.Parse(body).RootElement;
            var status = json.GetProperty("status").GetProperty("statusCode").GetString();
            var payUOrderId = json.GetProperty("orderId").GetString();

            return status == "SUCCESS" || status == "WARNING_CONTINUE_3DS"
                ? PaymentResult.Success(payUOrderId!, amount, status)
                : PaymentResult.Failure(status ?? "UNKNOWN", json.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BLIK payment exception");
            return PaymentResult.Failure("EXCEPTION", ex.Message);
        }
    }

    // ── 2. Preautoryzacja ────────────────────────────────────────────────────

    public async Task<PaymentResult> PreauthorizePayment(
        decimal amount, string orderId, string description)
    {
        // PayU obsługuje pre-auth przez dodanie flagi "authorize" w zamówieniu
        // Szczegóły: https://developers.payu.com/en/preauthorize.html
        _logger.LogInformation("Pre-authorizing {Amount} PLN for order {OrderId}", amount, orderId);
        // TODO: pełna implementacja analogiczna do InitiateBlikPayment
        await Task.CompletedTask;
        return PaymentResult.Success($"PREAUTH-{orderId}", amount, "PREAUTH_PENDING");
    }

    // ── 3. Capture ───────────────────────────────────────────────────────────

    public async Task<PaymentResult> CapturePreauthorization(
        string externalTransactionId, decimal amount)
    {
        _logger.LogInformation("Capturing preauth {TxId}", externalTransactionId);
        var token = await GetAccessTokenAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/api/v2_1/orders/{externalTransactionId}/capture");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode
            ? PaymentResult.Success(externalTransactionId, amount, "CAPTURED")
            : PaymentResult.Failure("CAPTURE_ERROR", $"HTTP {(int)response.StatusCode}");
    }

    // ── 4. Zwrot ─────────────────────────────────────────────────────────────

    public async Task<PaymentResult> RefundPayment(
        string externalTransactionId, decimal? partialAmount = null)
    {
        _logger.LogInformation("Refunding {TxId} amount={Amount}", externalTransactionId, partialAmount);
        var token = await GetAccessTokenAsync();

        var payload = partialAmount.HasValue
            ? new { refund = new { description = "Zwrot wypożyczenia", amount = (int)(partialAmount.Value * 100) } }
            : new { refund = new { description = "Zwrot wypożyczenia", amount = 0 } };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/api/v2_1/orders/{externalTransactionId}/refunds")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode
            ? PaymentResult.Success(externalTransactionId, partialAmount ?? 0m, "REFUNDED")
            : PaymentResult.Failure("REFUND_ERROR", $"HTTP {(int)response.StatusCode}");
    }

    // ── 5. Status ────────────────────────────────────────────────────────────

    public async Task<PaymentResult> CheckPaymentStatus(string externalTransactionId)
    {
        var token = await GetAccessTokenAsync();
        var response = await _httpClient.GetAsync(
            $"{_options.BaseUrl}/api/v2_1/orders/{externalTransactionId}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return PaymentResult.Failure("STATUS_ERROR", body);

        var json   = JsonDocument.Parse(body).RootElement;
        var orders = json.GetProperty("orders");
        var status = orders[0].GetProperty("status").GetString();
        return PaymentResult.Success(externalTransactionId, 0m, status ?? "UNKNOWN");
    }

    // ── OAuth2 token ─────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cachedAccessToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _cachedAccessToken;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "client_credentials",
            ["client_id"]     = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        });

        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/pl/standard/user/oauth/authorize", content);

        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        _cachedAccessToken = json.GetProperty("access_token").GetString()!;
        _tokenExpiry       = DateTime.UtcNow.AddSeconds(
            json.GetProperty("expires_in").GetInt32() - 30);

        return _cachedAccessToken;
    }
}

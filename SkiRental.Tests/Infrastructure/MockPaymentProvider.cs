using SkiRental.Logic.Interfaces;

namespace SkiRental.Tests.Infrastructure;

/// <summary>
/// Configurable in-memory payment provider for booking integration tests.
/// </summary>
public sealed class MockPaymentProvider : IPaymentProvider
{
    public bool BlikSucceeds { get; set; } = true;
    public bool PreauthSucceeds { get; set; } = true;
    public bool CaptureSucceeds { get; set; } = true;
    public bool RefundSucceeds { get; set; } = true;

    public int BlikCallCount { get; private set; }
    public int RefundCallCount { get; private set; }

    public Task<PaymentResult> InitiateBlikPayment(
        decimal amount, string blikCode, string orderId, string description)
    {
        BlikCallCount++;
        return Task.FromResult(BlikSucceeds
            ? PaymentResult.Success($"MOCK-BLIK-{orderId}", amount, "SUCCESS")
            : PaymentResult.Failure("MOCK_BLIK_FAIL", "BLIK rejected"));
    }

    public Task<PaymentResult> PreauthorizePayment(
        decimal amount, string orderId, string description)
    {
        return Task.FromResult(PreauthSucceeds
            ? PaymentResult.Success($"MOCK-PREAUTH-{orderId}", amount, "PREAUTH_SUCCESS")
            : PaymentResult.Failure("MOCK_PREAUTH_FAIL", "Preauth rejected"));
    }

    public Task<PaymentResult> CapturePreauthorization(
        string externalTransactionId, decimal amount)
    {
        return Task.FromResult(CaptureSucceeds
            ? PaymentResult.Success(externalTransactionId, amount, "CAPTURED")
            : PaymentResult.Failure("MOCK_CAPTURE_FAIL", "Capture failed"));
    }

    public Task<PaymentResult> RefundPayment(
        string externalTransactionId, decimal? partialAmount = null)
    {
        RefundCallCount++;
        return Task.FromResult(RefundSucceeds
            ? PaymentResult.Success(externalTransactionId, partialAmount ?? 0m, "REFUNDED")
            : PaymentResult.Failure("MOCK_REFUND_FAIL", "Refund failed"));
    }

    public Task<PaymentResult> CheckPaymentStatus(string externalTransactionId)
        => Task.FromResult(PaymentResult.Success(externalTransactionId, 100m, "COMPLETED"));
}

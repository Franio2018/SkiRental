using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkiRental.Logic.Services;
using SkiRental.Tests.Infrastructure;
using Xunit;

namespace SkiRental.Tests;

public class PayUPaymentProviderTests
{
    private static PayUPaymentProvider CreateProvider(FakeHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://secure.snd.payu.com/") },
            Options.Create(new PayUOptions
            {
                PosId = "test-pos",
                SignatureKey = "sig",
                ClientId = "client",
                ClientSecret = "secret",
                BaseUrl = "https://secure.snd.payu.com",
                NotifyUrl = "https://localhost/notify",
            }),
            NullLogger<PayUPaymentProvider>.Instance);

    [Fact]
    public async Task InitiateBlikPayment_SuccessfulOrder_ReturnsSuccess()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("oauth"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                    """{"access_token":"test-token","expires_in":3600,"token_type":"bearer"}""");
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                """
                {
                  "status": { "statusCode": "SUCCESS" },
                  "orderId": "PAYU-ORDER-123"
                }
                """);
        });

        var provider = CreateProvider(handler);
        var result = await provider.InitiateBlikPayment(100m, "123456", "42", "Test rental");

        result.IsSuccess.Should().BeTrue();
        result.ExternalTransactionId.Should().Be("PAYU-ORDER-123");
        result.Amount.Should().Be(100m);
        handler.Requests.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task InitiateBlikPayment_PayURejects_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("oauth"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                    """{"access_token":"test-token","expires_in":3600}""");
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                """
                {
                  "status": { "statusCode": "ERROR" },
                  "orderId": "PAYU-ORDER-ERR"
                }
                """);
        });

        var provider = CreateProvider(handler);
        var result = await provider.InitiateBlikPayment(50m, "654321", "99", "Fail case");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ERROR");
    }

    [Fact]
    public async Task PreauthorizePayment_Success_ReturnsPreauthSuccess()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("oauth"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                    """{"access_token":"tok","expires_in":3600}""");
            }

            return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                """
                {
                  "status": { "statusCode": "SUCCESS" },
                  "orderId": "PREAUTH-1"
                }
                """);
        });

        var provider = CreateProvider(handler);
        var result = await provider.PreauthorizePayment(200m, "10", "Preauth test");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be("PREAUTH_SUCCESS");
    }

    [Fact]
    public async Task CapturePreauthorization_Success_ReturnsCaptured()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("oauth"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                    """{"access_token":"tok","expires_in":3600}""");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var provider = CreateProvider(handler);
        var result = await provider.CapturePreauthorization("PREAUTH-1", 200m);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be("CAPTURED");
    }

    [Fact]
    public async Task RefundPayment_PartialAmount_Succeeds()
    {
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.Contains("oauth"))
            {
                return FakeHttpMessageHandler.Json(HttpStatusCode.OK,
                    """{"access_token":"tok","expires_in":3600}""");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var provider = CreateProvider(handler);
        var result = await provider.RefundPayment("ORDER-1", 25m);

        result.IsSuccess.Should().BeTrue();
        result.Amount.Should().Be(25m);
    }
}

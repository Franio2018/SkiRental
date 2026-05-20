using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SkiRental.Logic.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SkiRental.Logic.Services;

/// <summary>
/// Implementacja powiadomień: Email przez SMTP/SendGrid, SMS przez Twilio.
/// Każda metoda loguje status wysyłki i obsługuje błędy bez rzucania wyjątku
/// do wywołującego (fail-soft – booking nie może się wyłożyć z powodu powiadomień).
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    // ── Konfiguracja ─────────────────────────────────────────────────────────
    private string SmtpHost     => _config["Email:SmtpHost"]     ?? "localhost";
    private int    SmtpPort     => int.Parse(_config["Email:SmtpPort"] ?? "587");
    private bool   SmtpUseSsl   => bool.Parse(_config["Email:UseSsl"]  ?? "false");
    private string SmtpUser     => _config["Email:Username"]     ?? string.Empty;
    private string SmtpPassword => _config["Email:Password"]     ?? string.Empty;
    private string FromAddress  => _config["Email:FromAddress"]  ?? "noreply@skirent.pl";
    private string FromName     => _config["Email:FromName"]     ?? "SkiRental";

    private string TwilioSid    => _config["Sms:TwilioAccountSid"] ?? string.Empty;
    private string TwilioToken  => _config["Sms:TwilioAuthToken"]  ?? string.Empty;
    private string TwilioFrom   => _config["Sms:FromNumber"]        ?? string.Empty;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Email
    // ═════════════════════════════════════════════════════════════════════════

    public async Task SendEmailAsync(
        string to,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("SendEmailAsync: pusty adres odbiorcy – pomijam.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = payload.Subject;

            // Obsługa szablonów: zamień tokeny z TemplateData
            var body = ApplyTemplateData(payload.Body, payload.TemplateData);

            message.Body = new TextPart("html") { Text = WrapInHtmlTemplate(payload.Subject, body) };

            using var smtp = new SmtpClient();

            var secureOption = SmtpUseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            await smtp.ConnectAsync(SmtpHost, SmtpPort, secureOption, ct);

            if (!string.IsNullOrEmpty(SmtpUser))
                await smtp.AuthenticateAsync(SmtpUser, SmtpPassword, ct);

            var sendResult = await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);

            _logger.LogInformation(
                "EMAIL wysłany | Do: {To} | Temat: {Subject} | MessageId: {Id}",
                to, payload.Subject, sendResult);
        }
        catch (Exception ex)
        {
            // Fail-soft: logujemy błąd, ale nie rzucamy wyjątku
            _logger.LogError(ex,
                "Błąd wysyłania EMAIL do {To} (temat: {Subject})", to, payload.Subject);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SMS (Twilio)
    // ═════════════════════════════════════════════════════════════════════════

    public async Task SendSmsAsync(
        string phone,
        NotificationPayload payload,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            _logger.LogWarning("SendSmsAsync: pusty numer telefonu – pomijam.");
            return;
        }

        var smsBody = payload.SmsShortMessage ?? TruncateForSms(payload.Body);

        try
        {
            // Inicjalizacja klienta Twilio (idempotentna)
            if (!string.IsNullOrEmpty(TwilioSid) && !string.IsNullOrEmpty(TwilioToken))
                TwilioClient.Init(TwilioSid, TwilioToken);
            else
            {
                _logger.LogWarning("Brak konfiguracji Twilio – SMS nie zostanie wysłany.");
                return;
            }

            var message = await MessageResource.CreateAsync(
                body: smsBody,
                from: new Twilio.Types.PhoneNumber(TwilioFrom),
                to:   new Twilio.Types.PhoneNumber(NormalizePhone(phone)));

            _logger.LogInformation(
                "SMS wysłany | Do: {Phone} | SID: {Sid} | Status: {Status}",
                phone, message.Sid, message.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wysyłania SMS do {Phone}", phone);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private static string ApplyTemplateData(string template, Dictionary<string, string> data)
    {
        foreach (var (key, value) in data)
            template = template.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }

    private static string WrapInHtmlTemplate(string subject, string body) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>{subject}</title></head>
        <body style="font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:20px;">
          <div style="background:#0d6efd;padding:16px;border-radius:8px 8px 0 0;">
            <h2 style="color:white;margin:0">🎿 SkiRental</h2>
          </div>
          <div style="border:1px solid #dee2e6;border-top:0;padding:20px;border-radius:0 0 8px 8px;">
            {body}
          </div>
          <p style="color:#6c757d;font-size:12px;margin-top:12px;text-align:center;">
            SkiRental · Zakopane · kontakt@skirent.pl
          </p>
        </body>
        </html>
        """;

    private static string TruncateForSms(string text, int maxLength = 160)
        => text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    private static string NormalizePhone(string phone)
    {
        // Usuń spacje i myślniki; dodaj +48 jeśli brak kodu kraju
        var digits = new string(phone.Where(c => c == '+' || char.IsDigit(c)).ToArray());
        if (!digits.StartsWith('+') && digits.Length == 9)
            digits = "+48" + digits;
        return digits;
    }
}

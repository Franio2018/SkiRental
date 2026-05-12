using Microsoft.Extensions.Logging;
using SkiRental.Logic.Interfaces;

namespace SkiRental.Logic.Services;

/// <summary>
/// Simple notification service implementation for email/SMS notifications.
/// Used for worker override notifications and other customer communications.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, NotificationPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "EMAIL TO: {To} | SUBJECT: {Subject} | BODY: {Body}",
            to, payload.Subject, payload.Body);

        // TODO: Implement actual email sending (SMTP, SendGrid, etc.)
        // For now, this is a placeholder that logs the notification
        
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string phone, NotificationPayload payload, CancellationToken ct = default)
    {
        var message = payload.SmsShortMessage ?? payload.Body;
        
        _logger.LogInformation(
            "SMS TO: {Phone} | MESSAGE: {Message}",
            phone, message);

        // TODO: Implement actual SMS sending (Twilio, etc.)
        // For now, this is a placeholder that logs the notification
        
        return Task.CompletedTask;
    }
}

using MediSphere.Application.Interfaces;
using MediSphere.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging;

namespace MediSphere.Infrastructure.Services;

/// <summary>
/// Queue-backed implementation of IEmailSmsService. Enqueues messages for background delivery.
/// </summary>
public class QueuedEmailSmsService : IEmailSmsService
{
    private readonly EmailQueue _queue;
    private readonly ILogger<QueuedEmailSmsService> _logger;

    public QueuedEmailSmsService(EmailQueue queue, ILogger<QueuedEmailSmsService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        await _queue.EnqueueEmailAsync(new EmailMessage(toEmail, subject, bodyHtml));
        _logger.LogDebug("Email queued for delivery. Recipient={Recipient}, Subject={Subject}", toEmail, subject);
    }

    public async Task SendSmsAsync(string toPhoneNumber, string message)
    {
        await _queue.EnqueueSmsAsync(new SmsMessage(toPhoneNumber, message));
        _logger.LogDebug("SMS queued for delivery. Recipient={Recipient}", toPhoneNumber);
    }
}

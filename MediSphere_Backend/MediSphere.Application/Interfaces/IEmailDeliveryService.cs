namespace MediSphere.Application.Interfaces;

/// <summary>
/// Direct email/SMS delivery used by the background queue processor.
/// </summary>
public interface IEmailDeliveryService
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
    Task SendSmsAsync(string toPhoneNumber, string message);
}

using System.Threading.Tasks;

namespace MediSphere.Application.Interfaces;

public interface IEmailSmsService
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
    Task SendSmsAsync(string toPhoneNumber, string message);
}

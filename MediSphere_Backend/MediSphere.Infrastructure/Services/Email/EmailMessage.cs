namespace MediSphere.Infrastructure.Services.Email;

public sealed record EmailMessage(string ToEmail, string Subject, string BodyHtml);

public sealed record SmsMessage(string ToPhoneNumber, string Message);

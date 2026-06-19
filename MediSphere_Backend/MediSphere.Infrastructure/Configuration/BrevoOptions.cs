namespace MediSphere.Infrastructure.Configuration;

public class BrevoOptions
{
    public const string SectionName = "Brevo";

    public string? ApiKey { get; set; }
    public string SenderEmail { get; set; } = "support@medisphere.com";
    public string SenderName { get; set; } = "MediSphere Hospital";
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
}

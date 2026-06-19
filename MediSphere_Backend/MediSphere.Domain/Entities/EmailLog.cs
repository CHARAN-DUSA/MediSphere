using MediSphere.Domain.Enums;

namespace MediSphere.Domain.Entities;

public class EmailLog
{
    public int Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailDeliveryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

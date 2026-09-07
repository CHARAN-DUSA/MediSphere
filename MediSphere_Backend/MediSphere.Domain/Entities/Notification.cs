using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; } // Targets AppUser Id
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public string Type { get; set; } = "Reminder"; // Reminder, Broadcast, Alert
}

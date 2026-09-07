using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Domain.Enums;

namespace MediSphere.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<NotificationDto> SendNotificationAsync(int userId, string title, string message, string type = "Reminder");
    Task SendToRoleReferenceAsync(UserRole role, int referenceId, string title, string message, string type = "Reminder");
    Task BroadcastNotificationAsync(BroadcastDto dto);
}

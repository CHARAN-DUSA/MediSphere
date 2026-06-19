using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Notification;

namespace MediSphere.Application.Interfaces;

public interface ISignalRNotificationService
{
    Task NotifyQueueUpdateAsync(int doctorId, int departmentId);
    Task NotifyQueuePositionChangedAsync(int patientId, int tokenNumber, string status);
    Task NotifyConsultationStatusChangedAsync(int appointmentId, string status);
    Task NotifyUserNotificationAsync(int userId, NotificationDto notification);
    Task NotifyUserNotificationsUpdatedAsync(int userId);
    Task NotifyNotificationRemovedAsync(int userId, int notificationId);
    Task NotifyBroadcastAsync(IEnumerable<int> userIds);
}

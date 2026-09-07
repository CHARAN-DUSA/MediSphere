using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.Interfaces;
using MediSphere.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MediSphere.Infrastructure.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<QueueHub> _queueHubContext;
    private readonly IHubContext<VideoConsultationHub> _videoHubContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;

    public SignalRNotificationService(
        IHubContext<QueueHub> queueHubContext,
        IHubContext<VideoConsultationHub> videoHubContext,
        IHubContext<NotificationHub> notificationHubContext)
    {
        _queueHubContext = queueHubContext;
        _videoHubContext = videoHubContext;
        _notificationHubContext = notificationHubContext;
    }

    public async Task NotifyQueueUpdateAsync(int doctorId, int departmentId)
    {
        await _queueHubContext.Clients.Group($"doc_{doctorId}").SendAsync("QueueUpdated");
        await _queueHubContext.Clients.Group($"dept_{departmentId}").SendAsync("QueueUpdated");
    }

    public async Task NotifyQueuePositionChangedAsync(int patientId, int tokenNumber, string status)
    {
        await _queueHubContext.Clients.All.SendAsync($"QueuePositionChanged_{patientId}", tokenNumber, status);
    }

    public async Task NotifyConsultationStatusChangedAsync(int appointmentId, string status)
    {
        await _videoHubContext.Clients.Group($"room_{appointmentId}").SendAsync("ConsultationStatusChanged", status);
    }

    public async Task NotifyUserNotificationAsync(int userId, NotificationDto notification)
    {
        await _notificationHubContext.Clients.Group($"user_{userId}").SendAsync("NotificationReceived", notification);
    }

    public async Task NotifyUserNotificationsUpdatedAsync(int userId)
    {
        await _notificationHubContext.Clients.Group($"user_{userId}").SendAsync("NotificationsUpdated");
    }

    public async Task NotifyNotificationRemovedAsync(int userId, int notificationId)
    {
        await _notificationHubContext.Clients.Group($"user_{userId}").SendAsync("NotificationRemoved", notificationId);
    }

    public async Task NotifyBroadcastAsync(IEnumerable<int> userIds)
    {
        var tasks = userIds.Select(id =>
            _notificationHubContext.Clients.Group($"user_{id}").SendAsync("NotificationsUpdated"));
        await Task.WhenAll(tasks);
    }
}

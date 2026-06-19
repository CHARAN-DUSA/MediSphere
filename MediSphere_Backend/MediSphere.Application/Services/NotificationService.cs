using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _signalR;

    public NotificationService(IUnitOfWork unitOfWork, ISignalRNotificationService signalR)
    {
        _unitOfWork = unitOfWork;
        _signalR = signalR;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId)
    {
        var notifications = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var repo = _unitOfWork.Repository<Notification>();
        var notification = await repo.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        if (notification.UserId != userId)
            throw new UnauthorizedAccessException("You cannot dismiss another user's notification.");

        await repo.DeleteAsync(notification);
        await _unitOfWork.SaveChangesAsync();
        await _signalR.NotifyNotificationRemovedAsync(userId, notificationId);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.UserId == userId)
            .ToListAsync();

        if (notifications.Count == 0)
            return;

        var repo = _unitOfWork.Repository<Notification>();
        foreach (var notification in notifications)
            await repo.DeleteAsync(notification);

        await _unitOfWork.SaveChangesAsync();
        await _signalR.NotifyUserNotificationsUpdatedAsync(userId);
    }

    public async Task<NotificationDto> SendNotificationAsync(int userId, string title, string message, string type = "Reminder")
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false
        };

        var created = await _unitOfWork.Repository<Notification>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(created);
        await _signalR.NotifyUserNotificationAsync(userId, dto);
        return dto;
    }

    public async Task SendToRoleReferenceAsync(UserRole role, int referenceId, string title, string message, string type = "Reminder")
    {
        var userId = await ResolveAppUserIdAsync(role, referenceId);
        if (userId.HasValue)
            await SendNotificationAsync(userId.Value, title, message, type);
    }

    public async Task BroadcastNotificationAsync(BroadcastDto dto)
    {
        var users = await _unitOfWork.Repository<AppUser>().Query()
            .Where(u => u.IsActive)
            .ToListAsync();
        var repo = _unitOfWork.Repository<Notification>();
        var userIds = new List<int>();

        foreach (var user in users)
        {
            await repo.AddAsync(new Notification
            {
                UserId = user.Id,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false
            });
            userIds.Add(user.Id);
        }

        await _unitOfWork.SaveChangesAsync();
        await _signalR.NotifyBroadcastAsync(userIds);
    }

    private async Task<int?> ResolveAppUserIdAsync(UserRole role, int referenceId)
    {
        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.ReferenceId == referenceId && u.Role == role && u.IsActive);

        return user?.Id;
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        Type = n.Type,
        CreatedAt = n.CreatedAt
    };
}

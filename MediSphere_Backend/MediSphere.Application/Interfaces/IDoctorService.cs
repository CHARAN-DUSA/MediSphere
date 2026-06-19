using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Notification;

namespace MediSphere.Application.Interfaces;

public interface IDoctorService
{
    Task<PagedResult<DoctorDto>> GetDoctorsAsync(
        int page, 
        int pageSize, 
        string? specialty = null, 
        int? departmentId = null, 
        string? search = null,
        string? gender = null,
        string? location = null,
        string? language = null,
        decimal? minFee = null,
        decimal? maxFee = null,
        decimal? minRating = null,
        bool? isAvailable = null);

    Task<DoctorDto?> GetDoctorByIdAsync(int id);
    Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto dto);
    Task<DoctorDto> UpdateDoctorAsync(int id, UpdateDoctorDto dto);
    Task DeleteDoctorAsync(int id);
    Task<string> UploadProfileImageAsync(int doctorId, Stream imageStream, string fileName);
    
    // Availability & Schedules
    Task UpdateScheduleAsync(int doctorId, IEnumerable<DoctorScheduleDto> schedules);
    Task BlockSlotAsync(int doctorId, BlockSlotDto dto);
    Task SetVacationAsync(int doctorId, VacationDto dto);
    
    // Financials
    Task<DoctorEarningsDto> GetDoctorEarningsAsync(int doctorId);

    // Notifications
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId);
    Task MarkNotificationAsReadAsync(int notificationId, int userId);
    Task MarkAllNotificationsAsReadAsync(int userId);
}


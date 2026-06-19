using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.DTOs.Doctor;

namespace MediSphere.Application.Interfaces;

public interface IPatientService
{
    Task<PatientProfileDto?> GetProfileAsync(int patientId);
    Task<PatientProfileDto> UpdateProfileAsync(int patientId, PatientProfileDto dto);
    Task<IEnumerable<DoctorDto>> GetFavoritesAsync(int patientId);
    Task<bool> ToggleFavoriteAsync(int patientId, int doctorId);
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId);
    Task MarkNotificationAsReadAsync(int notificationId, int userId);
    Task MarkAllNotificationsAsReadAsync(int userId);

    Task<FamilyMemberDto> AddFamilyMemberAsync(int patientId, FamilyMemberDto dto);
    Task DeleteFamilyMemberAsync(int id);
}

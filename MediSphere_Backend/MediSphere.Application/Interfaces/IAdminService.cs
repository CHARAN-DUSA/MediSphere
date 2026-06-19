using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Admin;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Patient;

namespace MediSphere.Application.Interfaces;

public interface IAdminService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    
    // Doctor verification and management
    Task ApproveDoctorAsync(int doctorId, bool approve);
    Task SuspendDoctorAsync(int doctorId);
    Task BlockDoctorAsync(int doctorId);
    Task UnblockDoctorAsync(int doctorId);
    Task<IEnumerable<DoctorDto>> GetAllDoctorsForAdminAsync();
    
    // User blocking
    Task BlockUserAsync(string email, bool block);
    Task<IEnumerable<PatientProfileDto>> GetAllPatientsAsync();
    
    // Settings management
    Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync();
    Task UpdateSystemSettingAsync(SystemSettingDto dto);
    
    // Content management
    Task<IEnumerable<ContentItemDto>> GetContentItemsAsync(string? type = null);
    Task<ContentItemDto> UpsertContentItemAsync(ContentItemDto dto);
    Task DeleteContentItemAsync(int id);
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Admin;
using MediSphere.Application.DTOs.Common;
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
    Task<PagedResult<DoctorDto>> SearchDoctorsAsync(string? query, int page = 1, int pageSize = 10);
    Task<AdminDoctorDetailDto?> GetDoctorDetailAsync(int doctorId);
    Task DeleteDoctorAsync(int doctorId, string reason = "Admin deleted");
    
    // User & Patient management
    Task BlockUserAsync(string email, bool block);
    Task<IEnumerable<PatientProfileDto>> GetAllPatientsAsync();
    Task<PagedResult<PatientProfileDto>> SearchPatientsAsync(string? query, int page = 1, int pageSize = 10);
    Task<AdminPatientDetailDto?> GetPatientDetailAsync(int patientId);
    Task DeletePatientAsync(int patientId, string reason = "Admin deleted");
    
    // Refund Management
    Task<PagedResult<AdminRefundDto>> GetRefundsAsync(AdminRefundFilterDto filter);
    Task<AdminRefundSummaryDto> GetRefundSummaryAsync();
    Task<AdminRefundDetailDto?> GetRefundDetailAsync(int paymentTransactionId);
    Task<bool> RetryRefundAsync(int paymentTransactionId);

    // Settings management
    Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync();
    Task UpdateSystemSettingAsync(SystemSettingDto dto);
    
    // Content management
    Task<IEnumerable<ContentItemDto>> GetContentItemsAsync(string? type = null);
    Task<ContentItemDto> UpsertContentItemAsync(ContentItemDto dto);
    Task DeleteContentItemAsync(int id);
}

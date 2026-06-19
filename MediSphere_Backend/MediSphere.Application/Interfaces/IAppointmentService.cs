using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Common;

namespace MediSphere.Application.Interfaces;

public interface IAppointmentService
{
    Task<PagedResult<AppointmentDto>> GetAppointmentsAsync(int page, int pageSize, int? patientId = null, int? doctorId = null, string? status = null);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id);
    Task<AppointmentDto> CreateAppointmentAsync(int patientId, CreateAppointmentDto dto);
    Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentDto dto);
    Task CancelAppointmentAsync(int id, int requestingUserId, string role);
    Task<AppointmentDto> UpdateStatusAsync(int id, string status, string? notes = null);
    Task<IEnumerable<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date);
}

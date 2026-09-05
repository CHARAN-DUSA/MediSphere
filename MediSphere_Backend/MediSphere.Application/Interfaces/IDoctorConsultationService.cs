using MediSphere.Application.DTOs.Consultation;

namespace MediSphere.Application.Interfaces;

public interface IDoctorConsultationService
{
    Task<DoctorConsultationDto> GetConsultationAsync(
        int doctorId,
        int appointmentId);
}
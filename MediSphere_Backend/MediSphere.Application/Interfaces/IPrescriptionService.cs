using MediSphere.Application.DTOs.Prescription;

namespace MediSphere.Application.Interfaces;

public interface IPrescriptionService
{
    Task<PrescriptionDto> CreateAsync(
        int doctorId,
        CreatePrescriptionDto dto);

    Task<IEnumerable<PrescriptionDto>> GetPatientHistoryAsync(
        int patientId);

    Task<IEnumerable<PrescriptionDto>> GetDoctorPatientHistoryAsync(
        int doctorId,
        int patientId);

    Task<PrescriptionDto?> GetByAppointmentAsync(
    int appointmentId,
    int userId,
    string role);
}
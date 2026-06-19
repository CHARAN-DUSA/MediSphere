using MediSphere.Application.DTOs.MedicalRecord;

namespace MediSphere.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<IEnumerable<MedicalRecordDto>> GetPatientRecordsAsync(int patientId);
    Task<MedicalRecordDto> UploadRecordAsync(int patientId, int? appointmentId, Stream fileStream, string fileName, string description);
    Task DeleteRecordAsync(int id, int patientId);
}

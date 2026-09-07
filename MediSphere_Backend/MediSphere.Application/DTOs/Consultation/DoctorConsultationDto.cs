using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.MedicalRecord;
using MediSphere.Application.DTOs.Prescription;

namespace MediSphere.Application.DTOs.Consultation;

public class DoctorConsultationDto
{
    public AppointmentDto Appointment { get; set; } = null!;

    public DoctorPatientSummaryDto Patient { get; set; } = null!;

    public List<MedicalRecordDto> MedicalRecords { get; set; }
        = new();

    public List<AppointmentDto> PreviousConsultations { get; set; }
        = new();

    public List<PrescriptionDto> PreviousPrescriptions { get; set; }
        = new();
}
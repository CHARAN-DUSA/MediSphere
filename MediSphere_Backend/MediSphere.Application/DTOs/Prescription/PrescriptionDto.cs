namespace MediSphere.Application.DTOs.Prescription;

public class PrescriptionDto
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public int AppointmentId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string Diagnosis { get; set; } = string.Empty;

    public string ClinicalNotes { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<PrescriptionMedicineDto> Medicines { get; set; }
        = new();
}
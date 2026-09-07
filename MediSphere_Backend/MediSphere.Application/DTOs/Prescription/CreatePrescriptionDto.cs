namespace MediSphere.Application.DTOs.Prescription;

public class CreatePrescriptionDto
{
    public int AppointmentId { get; set; }

    public string Diagnosis { get; set; } = string.Empty;

    public string ClinicalNotes { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    public List<CreatePrescriptionMedicineDto> Medicines { get; set; }
        = new();
}
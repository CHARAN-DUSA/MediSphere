namespace MediSphere.Application.DTOs.Prescription;

public class CreatePrescriptionMedicineDto
{
    public string MedicineName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;
}
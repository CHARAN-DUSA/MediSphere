using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class PrescriptionMedicine : BaseEntity
{
    public int PrescriptionId { get; set; }

    public Prescription Prescription { get; set; } = null!;

    public string MedicineName { get; set; } = string.Empty;

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;
}
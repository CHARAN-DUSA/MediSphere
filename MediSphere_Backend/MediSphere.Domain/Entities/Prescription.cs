using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class Prescription : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public string Diagnosis { get; set; } = string.Empty;

    public string ClinicalNotes { get; set; } = string.Empty;

    public string Instructions { get; set; } = string.Empty;

    public DateTime? FollowUpDate { get; set; }

    public ICollection<PrescriptionMedicine> Medicines { get; set; }
        = new List<PrescriptionMedicine>();
}
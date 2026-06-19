using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class DoctorReview : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    
    public int Rating { get; set; } // 1 to 5
    public string Comment { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string DoctorResponse { get; set; } = string.Empty;
    public DateTime? ResponseCreatedAt { get; set; }
}

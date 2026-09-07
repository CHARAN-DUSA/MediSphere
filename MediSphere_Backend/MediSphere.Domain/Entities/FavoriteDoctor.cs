using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class FavoriteDoctor : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
}

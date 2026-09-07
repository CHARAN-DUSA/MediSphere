using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class PatientRewardLog : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int Points { get; set; }
    public string Action { get; set; } = string.Empty; // Booking, Review, Referral, Redemption
    public string Description { get; set; } = string.Empty;
}

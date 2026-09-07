using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class FamilyMember : BaseEntity
{
    public int PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Relation { get; set; } = string.Empty;

    public int Age { get; set; }
}
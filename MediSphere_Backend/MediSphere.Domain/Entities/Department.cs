using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}

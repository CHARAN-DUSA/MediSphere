namespace MediSphere.Application.DTOs.Patient;

public class FamilyMemberDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Relation { get; set; } = string.Empty;

    public int Age { get; set; }
}
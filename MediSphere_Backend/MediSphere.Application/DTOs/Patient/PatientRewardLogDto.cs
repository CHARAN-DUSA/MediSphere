using System;

namespace MediSphere.Application.DTOs.Patient;

public class PatientRewardLogDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int Points { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

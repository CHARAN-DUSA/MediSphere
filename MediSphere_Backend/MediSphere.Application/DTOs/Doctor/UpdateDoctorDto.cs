namespace MediSphere.Application.DTOs.Doctor;

public class UpdateDoctorDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal ConsultationFee { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LanguagesSpoken { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int DepartmentId { get; set; }
}

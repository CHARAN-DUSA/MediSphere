namespace MediSphere.Application.DTOs.Auth;

public class RegisterDoctorDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal ConsultationFee { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LanguagesSpoken { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string MedicalLicenseNumber { get; set; } = string.Empty;
    public string ProfileDocuments { get; set; } = string.Empty;
}

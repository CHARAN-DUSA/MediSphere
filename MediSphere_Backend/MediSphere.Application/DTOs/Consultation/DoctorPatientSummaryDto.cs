namespace MediSphere.Application.DTOs.Consultation;

public class DoctorPatientSummaryDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BloodGroup { get; set; } = string.Empty;

    public string MedicalHistory { get; set; } = string.Empty;
}
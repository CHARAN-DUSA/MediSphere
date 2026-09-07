using System.Collections.Generic;

namespace MediSphere.Application.DTOs.Patient;

public class PatientProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RewardPoints { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public List<FamilyMemberDto> FamilyMembers { get; set; } = new();
}


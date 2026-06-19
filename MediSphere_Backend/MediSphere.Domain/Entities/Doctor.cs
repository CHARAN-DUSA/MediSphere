using MediSphere.Domain.Common;
using MediSphere.Domain.Enums;

namespace MediSphere.Domain.Entities;

public class Doctor : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal ConsultationFee { get; set; }
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    public DoctorStatus ApprovalStatus { get; set; } = DoctorStatus.PendingReview;
    public string MedicalLicenseNumber { get; set; } = string.Empty;
    public string ProfileDocuments { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LanguagesSpoken { get; set; } = string.Empty;
    public decimal AverageRating { get; set; } = 0.0m;
    public int RatingCount { get; set; } = 0;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
}

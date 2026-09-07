namespace MediSphere.Application.DTOs.Appointment;

public class CreateAppointmentDto
{
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsFollowUp { get; set; }
    public int? PreviousAppointmentId { get; set; }
    public bool UseRewardPoints { get; set; }
    public string? ReferralCode { get; set; }
}

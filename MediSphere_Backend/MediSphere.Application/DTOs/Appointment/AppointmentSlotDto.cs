namespace MediSphere.Application.DTOs.Appointment;

public class AppointmentSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}
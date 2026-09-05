namespace MediSphere.Application.DTOs.Doctor;

public class DailyScheduleSlotDto
{
    public int? AppointmentId { get; set; }

    public DateTime Date { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? PatientName { get; set; }

    public string? Reason { get; set; }
}
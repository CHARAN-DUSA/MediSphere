namespace MediSphere.Application.DTOs.Appointment;

public class UpdateAppointmentDto
{
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
}

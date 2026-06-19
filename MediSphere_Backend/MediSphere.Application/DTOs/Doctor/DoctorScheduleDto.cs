using System;

namespace MediSphere.Application.DTOs.Doctor;

public class DoctorScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class BlockSlotDto
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class VacationDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

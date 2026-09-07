namespace MediSphere.Application.DTOs.Admin;

public class DashboardStatsDto
{
    public int TotalAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalDepartments { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal PendingPayouts { get; set; }
    public decimal CompletedPayouts { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public IEnumerable<DepartmentStatDto> DepartmentStats { get; set; } = [];
}

public class DepartmentStatDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}

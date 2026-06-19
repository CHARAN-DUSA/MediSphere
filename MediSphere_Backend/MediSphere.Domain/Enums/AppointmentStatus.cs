namespace MediSphere.Domain.Enums;

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    Rescheduled = 4,
    NoShow = 5,
    PendingPayment = 6
}

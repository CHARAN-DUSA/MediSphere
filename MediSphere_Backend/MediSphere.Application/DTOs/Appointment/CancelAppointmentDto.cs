using MediSphere.Domain.Enums;

namespace MediSphere.Application.DTOs.Appointment;

public class CancelAppointmentDto
{
    public CancellationReasonType ReasonType { get; set; } = CancellationReasonType.PatientRequest;
    public string? Reason { get; set; }
}

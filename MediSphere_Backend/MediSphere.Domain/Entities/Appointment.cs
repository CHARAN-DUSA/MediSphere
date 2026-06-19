using MediSphere.Domain.Common;
using MediSphere.Domain.Enums;

namespace MediSphere.Domain.Entities;

public class Appointment : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsFollowUp { get; set; }
    public int? PreviousAppointmentId { get; set; }
    public decimal Fee { get; set; }
    public string TelemedicineMeetingId { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public int QueueToken { get; set; } = 0;
    public string QueueStatus { get; set; } = "Waiting"; 
    public string PaymentStatus { get; set; } = "Unpaid"; 
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}

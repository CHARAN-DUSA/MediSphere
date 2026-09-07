using System;
using MediSphere.Domain.Enums;

namespace MediSphere.Application.DTOs.Appointment;

public class AppointmentDto
{
    public int Id { get; set; }
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsFollowUp { get; set; }
    public decimal Fee { get; set; }
    public string TelemedicineMeetingId { get; set; } = string.Empty;
    public string MeetingUrl { get; set; } = string.Empty;
    public int QueueToken { get; set; }
    public string QueueStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string RazorpayOrderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool HasReviewed { get; set; }

    // Blocked Slot & Cancellation/Refund Fields
    public bool IsBlockedSlot { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public int? CancelledByUserId { get; set; }
    public CancellationReasonType? CancellationReasonType { get; set; }
    public string? CancellationReason { get; set; }
    public decimal RefundAmount { get; set; }
    public string? RefundStatus { get; set; }
    public string? RazorpayRefundId { get; set; }
}
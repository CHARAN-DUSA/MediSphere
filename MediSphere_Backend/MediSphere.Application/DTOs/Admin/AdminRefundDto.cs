using System;
using System.Collections.Generic;
using MediSphere.Domain.Enums;

namespace MediSphere.Application.DTOs.Admin;

public class AdminRefundDto
{
    public int PaymentTransactionId { get; set; }
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorSpecialty { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public RefundStatus RefundStatus { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayRefundId { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? RefundInitiatedAt { get; set; }
    public DateTime? RefundCompletedAt { get; set; }
    public string? RefundFailureReason { get; set; }
    public string RefundDestination { get; set; } = "Original Payment Method";
    public DateTime CreatedAt { get; set; }
}

public class AdminRefundFilterDto
{
    public string? Query { get; set; }
    public RefundStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AdminRefundSummaryDto
{
    public int TotalRefunds { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
}

public class AdminRefundDetailDto : AdminRefundDto
{
    public string? DoctorDepartment { get; set; }
    public string? PatientPhone { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public decimal AdminCommission { get; set; }
    public decimal DoctorEarnings { get; set; }
    public decimal NetDoctorAmount { get; set; }
}

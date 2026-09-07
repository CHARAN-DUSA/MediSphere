using System;
using System.Collections.Generic;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Domain.Enums;

namespace MediSphere.Application.DTOs.Admin;

public class AdminDoctorDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal ConsultationFee { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public DoctorStatus ApprovalStatus { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal TotalGrossEarnings { get; set; }
    public decimal TotalNetEarnings { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalAdminCommission { get; set; }
    public List<AppointmentDto> RecentAppointments { get; set; } = new();
    public List<AdminDoctorBlockedSlotDto> BlockedSlots { get; set; } = new();
    public List<AdminDoctorTransactionDto> Transactions { get; set; } = new();
}

public class AdminDoctorBlockedSlotDto
{
    public int Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class AdminDoctorTransactionDto
{
    public int PaymentTransactionId { get; set; }
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal NetDoctorAmount { get; set; }
    public decimal AdminCommission { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RefundStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

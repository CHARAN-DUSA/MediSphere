using System;
using System.Collections.Generic;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Patient;

namespace MediSphere.Application.DTOs.Admin;

public class AdminPatientDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? MedicalHistory { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalSpend { get; set; }
    public decimal TotalRefunded { get; set; }
    public List<FamilyMemberDto> FamilyMembers { get; set; } = new();
    public List<AppointmentDto> RecentAppointments { get; set; } = new();
    public List<AdminPatientPaymentDto> PaymentHistory { get; set; } = new();
}

public class AdminPatientPaymentDto
{
    public int PaymentTransactionId { get; set; }
    public int AppointmentId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RefundStatus { get; set; } = string.Empty;
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpayRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
}

using System;
using System.Collections.Generic;
using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class Patient : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BloodGroup { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string MedicalHistory { get; set; } = string.Empty;
    public int RewardPoints { get; set; } = 0;
    public string ReferralCode { get; set; } = string.Empty;
    public string? ReferredByCode { get; set; }

    // Soft-Delete & Anonymization Audit
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public bool IsAnonymized { get; set; } = false;
    public DateTime? AnonymizedAt { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

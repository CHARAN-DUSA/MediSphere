using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public PatientService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

   public async Task<PatientProfileDto?> GetProfileAsync(int patientId)
{
    var patient = await _unitOfWork.Repository<Patient>()
        .Query()
        .Include(p => p.FamilyMembers)
        .FirstOrDefaultAsync(p => p.Id == patientId);

    if (patient == null)
        return null;

    return new PatientProfileDto
    {
        Id = patient.Id,
        Email = patient.Email,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        PhoneNumber = patient.PhoneNumber,
        Address = patient.Address,
        BloodGroup = patient.BloodGroup,
        MedicalHistory = patient.MedicalHistory,
        IsActive = patient.IsActive,
        RewardPoints = patient.RewardPoints,
        ReferralCode = patient.ReferralCode,

        FamilyMembers = patient.FamilyMembers
            .Select(f => new FamilyMemberDto
            {
                Id = f.Id,
                Name = f.Name,
                Relation = f.Relation,
                Age = f.Age
            })
            .ToList()
    };
}


    public async Task<PatientProfileDto> UpdateProfileAsync(
    int patientId,
    PatientProfileDto dto)
{
    var patient = await _unitOfWork
        .Repository<Patient>()
        .GetByIdAsync(patientId)
        ?? throw new KeyNotFoundException(
            $"Patient {patientId} not found.");

    patient.FirstName = dto.FirstName;
    patient.LastName = dto.LastName;
    patient.PhoneNumber = dto.PhoneNumber;
    patient.Address = dto.Address;
    patient.BloodGroup = dto.BloodGroup;
    patient.MedicalHistory = dto.MedicalHistory;

    
    patient.UpdatedAt = DateTime.UtcNow;

    await _unitOfWork.Repository<Patient>()
        .UpdateAsync(patient);

    var familyRepo = _unitOfWork.Repository<FamilyMember>();

var existingMembers = await familyRepo.Query()
    .Where(f => f.PatientId == patientId)
    .ToListAsync();

foreach (var member in existingMembers)
{
    await familyRepo.DeleteAsync(member);
}

foreach (var memberDto in dto.FamilyMembers)
{
    await familyRepo.AddAsync(new FamilyMember
    {
        PatientId = patientId,
        Name = memberDto.Name,
        Relation = memberDto.Relation,
        Age = memberDto.Age
    });
}

    await _unitOfWork.SaveChangesAsync();

    return await GetProfileAsync(patientId)
           ?? dto;
}

    public async Task<IEnumerable<DoctorDto>> GetFavoritesAsync(int patientId)
    {
        var favorites = await _unitOfWork.Repository<FavoriteDoctor>().Query()
            .Include(f => f.Doctor)
            .ThenInclude(d => d.Department)
            .Where(f => f.PatientId == patientId && f.Doctor.IsActive)
            .ToListAsync();

        return favorites.Select(f => new DoctorDto
        {
            Id = f.Doctor.Id,
            FirstName = f.Doctor.FirstName,
            LastName = f.Doctor.LastName,
            Email = f.Doctor.Email,
            PhoneNumber = f.Doctor.PhoneNumber,
            Specialty = f.Doctor.Specialty,
            Qualification = f.Doctor.Qualification,
            ExperienceYears = f.Doctor.ExperienceYears,
            ConsultationFee = f.Doctor.ConsultationFee,
            ProfileImageUrl = f.Doctor.ProfileImageUrl,
            Bio = f.Doctor.Bio,
            IsAvailable = f.Doctor.IsAvailable,
            IsApproved = f.Doctor.IsApproved,
            Gender = f.Doctor.Gender,
            Location = f.Doctor.Location,
            LanguagesSpoken = f.Doctor.LanguagesSpoken,
            AverageRating = f.Doctor.AverageRating,
            RatingCount = f.Doctor.RatingCount,
            DepartmentId = f.Doctor.DepartmentId,
            DepartmentName = f.Doctor.Department?.Name ?? string.Empty
        });
    }

    public async Task<bool> ToggleFavoriteAsync(int patientId, int doctorId)
    {
        var repo = _unitOfWork.Repository<FavoriteDoctor>();
        var existing = await repo.Query()
            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.DoctorId == doctorId);

        if (existing != null)
        {
            await repo.DeleteAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            return false; // Removed from favorites
        }
        else
        {
            var doctorExists = await _unitOfWork.Repository<Doctor>().ExistsAsync(d => d.Id == doctorId);
            if (!doctorExists) throw new KeyNotFoundException($"Doctor {doctorId} not found.");

            await repo.AddAsync(new FavoriteDoctor { PatientId = patientId, DoctorId = doctorId });
            await _unitOfWork.SaveChangesAsync();
            return true; // Added to favorites
        }
    }

    public Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId)
        => _notificationService.GetNotificationsAsync(userId);

    public Task MarkNotificationAsReadAsync(int notificationId, int userId)
        => _notificationService.MarkAsReadAsync(notificationId, userId);

    public Task MarkAllNotificationsAsReadAsync(int userId)
        => _notificationService.MarkAllAsReadAsync(userId);

    
public async Task<FamilyMemberDto> AddFamilyMemberAsync(
    int patientId,
    FamilyMemberDto dto)
{
    var member = new FamilyMember
    {
        PatientId = patientId,
        Name = dto.Name,
        Relation = dto.Relation,
        Age = dto.Age
    };

    await _unitOfWork.Repository<FamilyMember>()
        .AddAsync(member);

    await _unitOfWork.SaveChangesAsync();

    dto.Id = member.Id;

    return dto;
}

public async Task DeleteFamilyMemberAsync(int id)
{
    var member =
        await _unitOfWork
            .Repository<FamilyMember>()
            .GetByIdAsync(id);

    if (member == null)
        throw new KeyNotFoundException(
            "Family member not found.");

    await _unitOfWork
        .Repository<FamilyMember>()
        .DeleteAsync(member);

    await _unitOfWork.SaveChangesAsync();
}
}

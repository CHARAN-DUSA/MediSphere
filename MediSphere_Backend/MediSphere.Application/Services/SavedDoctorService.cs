using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class SavedDoctorService : ISavedDoctorService
{
    private readonly IUnitOfWork _unitOfWork;

    public SavedDoctorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SaveDoctorAsync(int patientId, int doctorId)
    {
        var existing = await _unitOfWork.Repository<FavoriteDoctor>().Query()
            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.DoctorId == doctorId);

        if (existing != null) return;

        var favorite = new FavoriteDoctor
        {
            PatientId = patientId,
            DoctorId = doctorId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<FavoriteDoctor>().AddAsync(favorite);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveSavedDoctorAsync(int patientId, int doctorId)
    {
        var existing = await _unitOfWork.Repository<FavoriteDoctor>().Query()
            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.DoctorId == doctorId);

        if (existing == null) return;

        await _unitOfWork.Repository<FavoriteDoctor>().DeleteAsync(existing);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<DoctorDto>> GetSavedDoctorsAsync(int patientId)
    {
        var favorites = await _unitOfWork.Repository<FavoriteDoctor>().Query()
            .Include(f => f.Doctor)
                .ThenInclude(d => d.Department)
            .Where(f => f.PatientId == patientId && f.Doctor.IsActive && f.Doctor.ApprovalStatus == DoctorStatus.Approved)
            .Select(f => f.Doctor)
            .ToListAsync();

        return favorites.Select(MapToDto);
    }

    public async Task<IEnumerable<DoctorDto>> SearchSavedDoctorsAsync(int patientId, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await GetSavedDoctorsAsync(patientId);
        }

        var favorites = await _unitOfWork.Repository<FavoriteDoctor>().Query()
            .Include(f => f.Doctor)
                .ThenInclude(d => d.Department)
            .Where(f => f.PatientId == patientId && f.Doctor.IsActive && f.Doctor.ApprovalStatus == DoctorStatus.Approved)
            .Select(f => f.Doctor)
            .Where(d => d.FirstName.Contains(search) || d.LastName.Contains(search) || d.Specialty.Contains(search))
            .ToListAsync();

        return favorites.Select(MapToDto);
    }

    private static DoctorDto MapToDto(Doctor d) => new()
    {
        Id = d.Id,
        FirstName = d.FirstName,
        LastName = d.LastName,
        Email = d.Email,
        PhoneNumber = d.PhoneNumber,
        Specialty = d.Specialty,
        Qualification = d.Qualification,
        ExperienceYears = d.ExperienceYears,
        ConsultationFee = d.ConsultationFee,
        ProfileImageUrl = d.ProfileImageUrl,
        Bio = d.Bio,
        IsAvailable = d.IsAvailable,
        IsApproved = d.IsApproved,
        Gender = d.Gender,
        Location = d.Location,
        LanguagesSpoken = d.LanguagesSpoken,
        AverageRating = d.AverageRating,
        RatingCount = d.RatingCount,
        DepartmentId = d.DepartmentId,
        DepartmentName = d.Department?.Name ?? string.Empty
    };
}

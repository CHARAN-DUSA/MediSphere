using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Admin;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSmsService _emailSms;

    public AdminService(IUnitOfWork unitOfWork, IEmailSmsService emailSms)
    {
        _unitOfWork = unitOfWork;
        _emailSms = emailSms;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var appointments = _unitOfWork.Repository<Appointment>().Query();
        var doctors = _unitOfWork.Repository<Doctor>().Query();
        var patients = _unitOfWork.Repository<Patient>().Query();
        var departments = _unitOfWork.Repository<Department>().Query();
        var transactions = _unitOfWork.Repository<PaymentTransaction>().Query();

        var totalRevenue = await transactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.GrossAmount) ?? 0m;
        var totalCommission = await transactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.AdminCommission) ?? 0m;

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyRevenue = await transactions
            .Where(t => t.Status == "Success" && t.CreatedAt >= startOfMonth)
            .SumAsync(t => (decimal?)t.GrossAmount) ?? 0m;

        var completedPayouts = await transactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.NetDoctorAmount) ?? 0m;
        var pendingPayouts = await transactions.Where(t => t.Status == "Pending").SumAsync(t => (decimal?)t.NetDoctorAmount) ?? 0m;

        var deptStats = await appointments
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .GroupBy(a => a.Doctor.Department.Name)
            .Select(g => new DepartmentStatDto { DepartmentName = g.Key, AppointmentCount = g.Count() })
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalAppointments = await appointments.CountAsync(),
            TodayAppointments = await appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
            TotalDoctors = await doctors.CountAsync(d => d.IsActive),
            TotalPatients = await patients.CountAsync(p => p.IsActive),
            TotalDepartments = await departments.CountAsync(d => d.IsActive),
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            MonthlyRevenue = monthlyRevenue,
            PendingPayouts = pendingPayouts,
            CompletedPayouts = completedPayouts,
            PendingAppointments = await appointments.CountAsync(a => a.Status == AppointmentStatus.Pending),
            CompletedAppointments = await appointments.CountAsync(a => a.Status == AppointmentStatus.Completed),
            DepartmentStats = deptStats
        };
    }

    public async Task ApproveDoctorAsync(int doctorId, bool approve)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        doctor.IsApproved = approve;
        doctor.ApprovalStatus = approve ? DoctorStatus.Approved : DoctorStatus.Rejected;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        string doctorName = $"{doctor.FirstName} {doctor.LastName}";
        string emailBody = approve
            ? MediSphere.Application.Common.EmailTemplates.BuildDoctorApprovedEmail(doctorName, doctor.Specialty)
            : MediSphere.Application.Common.EmailTemplates.BuildDoctorRejectedEmail(doctorName, "Compliance/documentation verification failed.");

        string subject = approve ? "MediSphere Profile Approved" : "MediSphere Registration Application Update";

        await _emailSms.SendEmailAsync(doctor.Email, subject, emailBody);
    }

    public async Task SuspendDoctorAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        doctor.ApprovalStatus = DoctorStatus.Suspended;
        doctor.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);

        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.Email == doctor.Email);
        if (user != null)
        {
            user.IsActive = false;
            await _unitOfWork.Repository<AppUser>().UpdateAsync(user);
        }
        await _unitOfWork.SaveChangesAsync();

        string doctorName = $"{doctor.FirstName} {doctor.LastName}";
        string emailBody = MediSphere.Application.Common.EmailTemplates.BuildDoctorSuspendedEmail(
            doctorName, "Suspended due to administrative policy review.", false);

        await _emailSms.SendEmailAsync(doctor.Email, "MediSphere Account Suspended", emailBody);
    }

    public async Task BlockDoctorAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        doctor.ApprovalStatus = DoctorStatus.Blocked;
        doctor.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);

        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.Email == doctor.Email);
        if (user != null)
        {
            user.IsActive = false;
            await _unitOfWork.Repository<AppUser>().UpdateAsync(user);
        }
        await _unitOfWork.SaveChangesAsync();

        string doctorName = $"{doctor.FirstName} {doctor.LastName}";
        string emailBody = MediSphere.Application.Common.EmailTemplates.BuildDoctorSuspendedEmail(
            doctorName, "Blocked due to policy or regulatory violations.", true);

        await _emailSms.SendEmailAsync(doctor.Email, "MediSphere Account Blocked", emailBody);
    }

    public async Task UnblockDoctorAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        doctor.ApprovalStatus = DoctorStatus.Approved;
        doctor.IsActive = true;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);

        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.Email == doctor.Email);
        if (user != null)
        {
            user.IsActive = true;
            await _unitOfWork.Repository<AppUser>().UpdateAsync(user);
        }
        await _unitOfWork.SaveChangesAsync();

        // ✅ THIS WAS THE MISSING CALL
        string doctorName = $"{doctor.FirstName} {doctor.LastName}";
        string emailBody = MediSphere.Application.Common.EmailTemplates.BuildDoctorUnblockedEmail(doctorName);
        await _emailSms.SendEmailAsync(doctor.Email, "MediSphere Account Reinstated", emailBody);
    }

    public async Task<IEnumerable<DoctorDto>> GetAllDoctorsForAdminAsync()
    {
        var doctors = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .ToListAsync();

        return doctors.Select(d => new DoctorDto
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
            IsActive = d.IsActive,
            ApprovalStatus = d.ApprovalStatus.ToString(),
            Gender = d.Gender,
            Location = d.Location,
            LanguagesSpoken = d.LanguagesSpoken,
            AverageRating = d.AverageRating,
            RatingCount = d.RatingCount,
            DepartmentId = d.DepartmentId,
            DepartmentName = d.Department?.Name ?? string.Empty
        });
    }

    public async Task BlockUserAsync(string email, bool block)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var user = await userRepo.Query().FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new KeyNotFoundException($"User with email {email} not found.");

        user.IsActive = !block;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepo.UpdateAsync(user);

        // Also update corresponding Patient or Doctor
        if (user.Role == UserRole.Patient && user.ReferenceId.HasValue)
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(user.ReferenceId.Value);
            if (patient != null)
            {
                patient.IsActive = !block;
                patient.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Patient>().UpdateAsync(patient);
            }
        }
        else if (user.Role == UserRole.Doctor && user.ReferenceId.HasValue)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(user.ReferenceId.Value);
            if (doctor != null)
            {
                doctor.IsActive = !block;
                doctor.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<PatientProfileDto>> GetAllPatientsAsync()
    {
        var patients = await _unitOfWork.Repository<Patient>()
            .Query()
            .Include(p => p.FamilyMembers)
            .ToListAsync();

        return patients.Select(p => new PatientProfileDto
        {
            Id = p.Id,
            Email = p.Email,
            FirstName = p.FirstName,
            LastName = p.LastName,
            PhoneNumber = p.PhoneNumber,
            Address = p.Address,
            BloodGroup = p.BloodGroup,
            MedicalHistory = p.MedicalHistory,
            IsActive = p.IsActive,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            CreatedAt = p.CreatedAt,

            FamilyMembers = p.FamilyMembers
                .Select(f => new FamilyMemberDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Relation = f.Relation,
                    Age = f.Age
                })
                .ToList()
        });
    }

    public async Task<IEnumerable<SystemSettingDto>> GetSystemSettingsAsync()
    {
        var settings = await _unitOfWork.Repository<SystemSetting>().GetAllAsync();
        return settings.Select(s => new SystemSettingDto
        {
            Key = s.Key,
            Value = s.Value,
            Description = s.Description
        });
    }

    public async Task UpdateSystemSettingAsync(SystemSettingDto dto)
    {
        var repo = _unitOfWork.Repository<SystemSetting>();
        var setting = await repo.Query().FirstOrDefaultAsync(s => s.Key == dto.Key);
        if (setting != null)
        {
            setting.Value = dto.Value;
            setting.Description = dto.Description;
            setting.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(setting);
        }
        else
        {
            await repo.AddAsync(new SystemSetting
            {
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description
            });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContentItemDto>> GetContentItemsAsync(string? type = null)
    {
        var query = _unitOfWork.Repository<ContentItem>().Query();
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(c => c.Type == type);
        }
        var items = await query.OrderBy(c => c.Order).ToListAsync();
        return items.Select(c => new ContentItemDto
        {
            Id = c.Id,
            Type = c.Type,
            Title = c.Title,
            Content = c.Content,
            ImageUrl = c.ImageUrl,
            Order = c.Order
        });
    }

    public async Task<ContentItemDto> UpsertContentItemAsync(ContentItemDto dto)
    {
        var repo = _unitOfWork.Repository<ContentItem>();
        if (dto.Id > 0)
        {
            var item = await repo.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"Content item {dto.Id} not found.");
            item.Type = dto.Type;
            item.Title = dto.Title;
            item.Content = dto.Content;
            item.ImageUrl = dto.ImageUrl;
            item.Order = dto.Order;
            item.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
            return dto;
        }
        else
        {
            var item = new ContentItem
            {
                Type = dto.Type,
                Title = dto.Title,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                Order = dto.Order
            };
            var created = await repo.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            dto.Id = created.Id;
            return dto;
        }
    }

    public async Task DeleteContentItemAsync(int id)
    {
        var repo = _unitOfWork.Repository<ContentItem>();
        var item = await repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Content item {id} not found.");
        await repo.DeleteAsync(item);
        await _unitOfWork.SaveChangesAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Admin;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSmsService _emailSms;
    private readonly ICacheService _cacheService;
    private readonly IAccountDeletionService _accountDeletionService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUnitOfWork unitOfWork, 
        IEmailSmsService emailSms,
        ICacheService cacheService,
        IAccountDeletionService accountDeletionService,
        IPaymentService paymentService,
        ILogger<AdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSms = emailSms;
        _cacheService = cacheService;
        _accountDeletionService = accountDeletionService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var cacheKey = "medisphere:admin:dashboard";
        var cached = await _cacheService.GetAsync<DashboardStatsDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var appointments = _unitOfWork.Repository<Appointment>().Query();
        var doctors = _unitOfWork.Repository<Doctor>().Query();
        var patients = _unitOfWork.Repository<Patient>().Query();
        var departments = _unitOfWork.Repository<Department>().Query();
        var transactions = _unitOfWork.Repository<PaymentTransaction>().Query();

        var successfulTransactions = await transactions.Where(t => t.Status == "Success").ToListAsync();

        var totalGross = successfulTransactions.Sum(t => t.GrossAmount);
        var totalRefunded = successfulTransactions.Sum(t => t.RefundAmount);
        var totalRevenue = totalGross - totalRefunded;
        var refundedTransactionsCount = successfulTransactions.Count(t =>
            t.RefundStatus == RefundStatus.Refunded ||
            t.RefundStatus == RefundStatus.Simulated ||
            t.RefundStatus == RefundStatus.PartiallyRefunded);

        var totalCommission = successfulTransactions
            .Where(t => t.RefundStatus != RefundStatus.Refunded && t.RefundStatus != RefundStatus.Simulated)
            .Sum(t => t.AdminCommission);

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthlyRevenue = successfulTransactions
            .Where(t => t.CreatedAt >= startOfMonth)
            .Sum(t => t.GrossAmount - t.RefundAmount);

        var completedPayouts = successfulTransactions
            .Where(t => t.RefundStatus != RefundStatus.Refunded && t.RefundStatus != RefundStatus.Simulated)
            .Sum(t => t.NetDoctorAmount);

        var pendingPayouts = await transactions
            .Where(t => t.Status == "Pending")
            .SumAsync(t => (decimal?)t.NetDoctorAmount) ?? 0m;

        var deptStats = await appointments
            .Where(a => !a.IsBlockedSlot)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .GroupBy(a => a.Doctor.Department.Name)
            .Select(g => new DepartmentStatDto { DepartmentName = g.Key, AppointmentCount = g.Count() })
            .ToListAsync();

        var stats = new DashboardStatsDto
        {
            TotalAppointments = await appointments.CountAsync(a => !a.IsBlockedSlot),
            TodayAppointments = await appointments.CountAsync(a => !a.IsBlockedSlot && a.AppointmentDate.Date == DateTime.Today),
            TotalDoctors = await doctors.CountAsync(d => d.IsActive && !d.IsDeleted),
            TotalPatients = await patients.CountAsync(p => p.IsActive && !p.IsDeleted),
            TotalDepartments = await departments.CountAsync(d => d.IsActive),
            TotalRevenue = totalRevenue,
            TotalGrossBeforeRefunds = totalGross,
            TotalRefundedAmount = totalRefunded,
            RefundedTransactionsCount = refundedTransactionsCount,
            TotalCommission = totalCommission,
            MonthlyRevenue = monthlyRevenue,
            PendingPayouts = pendingPayouts,
            CompletedPayouts = completedPayouts,
            PendingAppointments = await appointments.CountAsync(a => !a.IsBlockedSlot && a.Status == AppointmentStatus.Pending),
            CompletedAppointments = await appointments.CountAsync(a => !a.IsBlockedSlot && a.Status == AppointmentStatus.Completed),
            DepartmentStats = deptStats
        };

        await _cacheService.SetAsync(cacheKey, stats, TimeSpan.FromSeconds(90));
        return stats;
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

        await InvalidateAdminDoctorCachesAsync(doctorId);

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

        await InvalidateAdminDoctorCachesAsync(doctorId);

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

        await InvalidateAdminDoctorCachesAsync(doctorId);

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

        await InvalidateAdminDoctorCachesAsync(doctorId);

        string doctorName = $"{doctor.FirstName} {doctor.LastName}";
        string emailBody = MediSphere.Application.Common.EmailTemplates.BuildDoctorUnblockedEmail(doctorName);
        await _emailSms.SendEmailAsync(doctor.Email, "MediSphere Account Reinstated", emailBody);
    }

    private async Task InvalidateAdminDoctorCachesAsync(int doctorId)
    {
        await _cacheService.RemoveAsync("medisphere:admin:dashboard");
        await _cacheService.RemoveAsync($"medisphere:doctor:{doctorId}");
        await _cacheService.RemoveByPrefixAsync("medisphere:doctors:list:");
        await _cacheService.RemoveByPrefixAsync("medisphere:smartrecommend:");
        await _cacheService.RemoveByPrefixAsync("medisphere:home:");
    }

    public async Task<IEnumerable<DoctorDto>> GetAllDoctorsForAdminAsync()
    {
        var doctors = await _unitOfWork.Repository<Doctor>().Query()
            .Where(d => !d.IsDeleted)
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

    public async Task<PagedResult<DoctorDto>> SearchDoctorsAsync(string? query, int page = 1, int pageSize = 10)
    {
        var q = _unitOfWork.Repository<Doctor>().Query()
            .Where(d => !d.IsDeleted)
            .Include(d => d.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.Trim().ToLower();
            q = q.Where(d => d.FirstName.ToLower().Contains(lower) ||
                             d.LastName.ToLower().Contains(lower) ||
                             d.Email.ToLower().Contains(lower) ||
                             d.PhoneNumber.Contains(lower) ||
                             d.Specialty.ToLower().Contains(lower) ||
                             (d.Department != null && d.Department.Name.ToLower().Contains(lower)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DoctorDto>
        {
            Items = items.Select(d => new DoctorDto
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
            }),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminDoctorDetailDto?> GetDoctorDetailAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        if (doctor == null) return null;

        var appointments = await _unitOfWork.Repository<Appointment>().Query()
            .Where(a => a.DoctorId == doctorId)
            .Include(a => a.Patient)
            .Include(a => a.PaymentTransactions)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        var regularAppointments = appointments.Where(a => !a.IsBlockedSlot).ToList();
        var blockedSlots = appointments.Where(a => a.IsBlockedSlot).ToList();

        var successfulTransactions = regularAppointments
            .SelectMany(a => a.PaymentTransactions)
            .Where(t => t.Status == "Success")
            .ToList();

        // Genuine revenue is scoped to consultations that were actually
        // completed and not refunded — a cancelled appointment (refund
        // pending, processing, or failed) must not inflate gross revenue
        // or the doctor's payout, even though the payment itself succeeded.
        var earnedTransactions = regularAppointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .SelectMany(a => a.PaymentTransactions)
            .Where(t => t.Status == "Success"
                     && t.RefundStatus != RefundStatus.Refunded
                     && t.RefundStatus != RefundStatus.Simulated)
            .ToList();

        var totalGross = earnedTransactions.Sum(t => t.GrossAmount);
        var totalRefunds = successfulTransactions
            .Where(t => t.RefundStatus == RefundStatus.Refunded
                     || t.RefundStatus == RefundStatus.Simulated
                     || t.RefundStatus == RefundStatus.PartiallyRefunded)
            .Sum(t => t.RefundAmount);
        var refundedAppointmentsCount = successfulTransactions
            .Count(t => t.RefundStatus == RefundStatus.Refunded
                     || t.RefundStatus == RefundStatus.Simulated
                     || t.RefundStatus == RefundStatus.PartiallyRefunded);
        var totalNetDoctor = earnedTransactions.Sum(t => t.NetDoctorAmount);
        var totalCommission = earnedTransactions.Sum(t => t.AdminCommission);

        return new AdminDoctorDetailDto
        {
            Id = doctor.Id,
            Email = doctor.Email,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            PhoneNumber = doctor.PhoneNumber,
            Specialty = doctor.Specialty,
            Qualification = doctor.Qualification,
            ExperienceYears = doctor.ExperienceYears,
            ConsultationFee = doctor.ConsultationFee,
            DepartmentName = doctor.Department?.Name ?? string.Empty,
            IsActive = doctor.IsActive,
            IsApproved = doctor.IsApproved,
            ApprovalStatus = doctor.ApprovalStatus,
            IsDeleted = doctor.IsDeleted,
            CreatedAt = doctor.CreatedAt,
            TotalAppointments = regularAppointments.Count,
            CompletedAppointments = regularAppointments.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = regularAppointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            TotalGrossEarnings = totalGross,
            TotalNetEarnings = totalNetDoctor,
            TotalRefunds = totalRefunds,
            RefundedAppointmentsCount = refundedAppointmentsCount,
            TotalAdminCommission = totalCommission,
            RecentAppointments = regularAppointments.Take(15).Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "Unknown",
                DoctorId = a.DoctorId,
                DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                DepartmentName = doctor.Department?.Name ?? string.Empty,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status.ToString(),
                Reason = a.Reason,
                Fee = a.Fee,
                PaymentStatus = a.PaymentStatus,
                CreatedAt = a.CreatedAt,
                CancelledAt = a.CancelledAt,
                CancelledBy = a.CancelledBy,
                CancellationReason = a.CancellationReason,
                RefundAmount = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RefundAmount ?? 0m,
                RefundStatus = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RefundStatus.ToString(),
                RazorpayRefundId = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RazorpayRefundId
            }).ToList(),
            BlockedSlots = blockedSlots.Select(b => new AdminDoctorBlockedSlotDto
            {
                Id = b.Id,
                AppointmentDate = b.AppointmentDate,
                StartTime = b.StartTime.ToString(@"hh\:mm"),
                EndTime = b.EndTime.ToString(@"hh\:mm"),
                Reason = b.Reason
            }).ToList(),
            Transactions = successfulTransactions.Take(20).Select(t => new AdminDoctorTransactionDto
            {
                PaymentTransactionId = t.Id,
                AppointmentId = t.AppointmentId,
                PatientName = regularAppointments.FirstOrDefault(a => a.Id == t.AppointmentId)?.Patient != null
                    ? $"{regularAppointments.FirstOrDefault(a => a.Id == t.AppointmentId)!.Patient.FirstName} {regularAppointments.FirstOrDefault(a => a.Id == t.AppointmentId)!.Patient.LastName}"
                    : "Patient",
                GrossAmount = t.GrossAmount,
                NetDoctorAmount = t.NetDoctorAmount,
                AdminCommission = t.AdminCommission,
                RefundAmount = t.RefundAmount,
                Status = t.Status,
                RefundStatus = t.RefundStatus.ToString(),
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }

    public async Task DeleteDoctorAsync(int doctorId, string reason = "Admin deleted")
    {
        await _accountDeletionService.DeleteDoctorAccountAsync(doctorId, reason);
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
            .Where(p => !p.IsDeleted)
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

    public async Task<PagedResult<PatientProfileDto>> SearchPatientsAsync(string? query, int page = 1, int pageSize = 10)
    {
        var q = _unitOfWork.Repository<Patient>().Query()
            .Where(p => !p.IsDeleted)
            .Include(p => p.FamilyMembers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.Trim().ToLower();
            q = q.Where(p => p.FirstName.ToLower().Contains(lower) ||
                             p.LastName.ToLower().Contains(lower) ||
                             p.Email.ToLower().Contains(lower) ||
                             p.PhoneNumber.Contains(lower));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PatientProfileDto>
        {
            Items = items.Select(p => new PatientProfileDto
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
            }),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminPatientDetailDto?> GetPatientDetailAsync(int patientId)
    {
        var patient = await _unitOfWork.Repository<Patient>().Query()
            .Include(p => p.FamilyMembers)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null) return null;

        var appointments = await _unitOfWork.Repository<Appointment>().Query()
            .Where(a => a.PatientId == patientId && !a.IsBlockedSlot)
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Include(a => a.PaymentTransactions)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        var allTransactions = appointments
            .SelectMany(a => a.PaymentTransactions)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        var totalSpend = allTransactions
            .Where(t => t.Status == "Success" && t.RefundStatus != RefundStatus.Refunded && t.RefundStatus != RefundStatus.Simulated)
            .Sum(t => t.GrossAmount);

        var totalRefunded = allTransactions
            .Where(t => t.RefundStatus == RefundStatus.Refunded || t.RefundStatus == RefundStatus.Simulated || t.RefundStatus == RefundStatus.PartiallyRefunded)
            .Sum(t => t.RefundAmount);

        return new AdminPatientDetailDto
        {
            Id = patient.Id,
            Email = patient.Email,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            PhoneNumber = patient.PhoneNumber,
            Address = patient.Address,
            BloodGroup = patient.BloodGroup,
            MedicalHistory = patient.MedicalHistory,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            IsActive = patient.IsActive,
            IsDeleted = patient.IsDeleted,
            CreatedAt = patient.CreatedAt,
            TotalAppointments = appointments.Count,
            TotalSpend = totalSpend,
            TotalRefunded = totalRefunded,
            FamilyMembers = patient.FamilyMembers.Select(f => new FamilyMemberDto
            {
                Id = f.Id,
                Name = f.Name,
                Relation = f.Relation,
                Age = f.Age
            }).ToList(),
            RecentAppointments = appointments.Take(15).Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                DoctorId = a.DoctorId,
                DoctorName = $"Dr. {a.Doctor?.FirstName} {a.Doctor?.LastName}",
                DepartmentName = a.Doctor?.Department?.Name ?? string.Empty,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status.ToString(),
                Reason = a.Reason,
                Fee = a.Fee,
                PaymentStatus = a.PaymentStatus,
                CreatedAt = a.CreatedAt,
                CancelledAt = a.CancelledAt,
                CancelledBy = a.CancelledBy,
                CancellationReason = a.CancellationReason,
                RefundAmount = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RefundAmount ?? 0m,
                RefundStatus = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RefundStatus.ToString(),
                RazorpayRefundId = a.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.RazorpayRefundId
            }).ToList(),
            PaymentHistory = allTransactions.Select(t =>
            {
                var appt = appointments.FirstOrDefault(a => a.Id == t.AppointmentId);
                return new AdminPatientPaymentDto
                {
                    PaymentTransactionId = t.Id,
                    AppointmentId = t.AppointmentId,
                    DoctorName = appt?.Doctor != null ? $"Dr. {appt.Doctor.FirstName} {appt.Doctor.LastName}" : "Doctor",
                    GrossAmount = t.GrossAmount,
                    RefundAmount = t.RefundAmount,
                    Status = t.Status,
                    RefundStatus = t.RefundStatus.ToString(),
                    RazorpayPaymentId = t.RazorpayPaymentId,
                    RazorpayRefundId = t.RazorpayRefundId,
                    CreatedAt = t.CreatedAt
                };
            }).ToList()
        };
    }

    public async Task DeletePatientAsync(int patientId, string reason = "Admin deleted")
    {
        await _accountDeletionService.DeletePatientAccountAsync(patientId, reason);
    }

    // =========================================================
    // REFUND MANAGEMENT IMPLEMENTATION
    // =========================================================
    public async Task<PagedResult<AdminRefundDto>> GetRefundsAsync(AdminRefundFilterDto filter)
    {
        var q = _unitOfWork.Repository<PaymentTransaction>().Query()
            .Include(pt => pt.Appointment).ThenInclude(a => a.Patient)
            .Include(pt => pt.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.Department)
            .Where(pt => pt.RefundStatus != RefundStatus.NotApplicable || pt.RefundAmount > 0 || pt.Appointment.Status == AppointmentStatus.Cancelled)
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            q = q.Where(pt => pt.RefundStatus == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            q = q.Where(pt => pt.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            q = q.Where(pt => pt.CreatedAt <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var lower = filter.Query.Trim().ToLower();
            q = q.Where(pt => (pt.Appointment.Patient != null && (pt.Appointment.Patient.FirstName.ToLower().Contains(lower) || pt.Appointment.Patient.LastName.ToLower().Contains(lower) || pt.Appointment.Patient.Email.ToLower().Contains(lower)))
                           || (pt.Appointment.Doctor != null && (pt.Appointment.Doctor.FirstName.ToLower().Contains(lower) || pt.Appointment.Doctor.LastName.ToLower().Contains(lower)))
                           || pt.RazorpayPaymentId.Contains(lower)
                           || (pt.RazorpayRefundId != null && pt.RazorpayRefundId.Contains(lower))
                           || pt.AppointmentId.ToString() == lower
                           || pt.Id.ToString() == lower);
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(pt => pt.RefundInitiatedAt ?? pt.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AdminRefundDto>
        {
            Items = items.Select(pt => new AdminRefundDto
            {
                PaymentTransactionId = pt.Id,
                AppointmentId = pt.AppointmentId,
                PatientName = pt.Appointment.Patient != null ? $"{pt.Appointment.Patient.FirstName} {pt.Appointment.Patient.LastName}" : "Unknown",
                PatientEmail = pt.Appointment.Patient?.Email ?? "",
                DoctorName = pt.Appointment.Doctor != null ? $"Dr. {pt.Appointment.Doctor.FirstName} {pt.Appointment.Doctor.LastName}" : "Unknown",
                DoctorSpecialty = pt.Appointment.Doctor?.Specialty ?? "",
                OriginalAmount = pt.GrossAmount > 0 ? pt.GrossAmount : pt.Amount,
                RefundAmount = pt.RefundAmount,
                PaymentStatus = pt.Status,
                RefundStatus = pt.RefundStatus,
                RazorpayPaymentId = pt.RazorpayPaymentId,
                RazorpayRefundId = pt.RazorpayRefundId,
                CancellationReason = pt.Appointment.CancellationReason,
                CancelledBy = pt.Appointment.CancelledBy,
                CancelledAt = pt.Appointment.CancelledAt,
                RefundInitiatedAt = pt.RefundInitiatedAt,
                RefundCompletedAt = pt.RefundCompletedAt,
                RefundFailureReason = pt.RefundFailureReason,
                RefundDestination = "Original Payment Method",
                CreatedAt = pt.CreatedAt
            }),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AdminRefundSummaryDto> GetRefundSummaryAsync()
    {
        var transactions = await _unitOfWork.Repository<PaymentTransaction>().Query()
            .Include(pt => pt.Appointment)
            .Where(pt => pt.RefundStatus != RefundStatus.NotApplicable || pt.RefundAmount > 0 || pt.Appointment.Status == AppointmentStatus.Cancelled)
            .ToListAsync();

        return new AdminRefundSummaryDto
        {
            TotalRefunds = transactions.Count(t => t.RefundStatus != RefundStatus.NotApplicable),
            TotalRefundedAmount = transactions
                .Where(t => t.RefundStatus == RefundStatus.Refunded || t.RefundStatus == RefundStatus.Simulated || t.RefundStatus == RefundStatus.PartiallyRefunded)
                .Sum(t => t.RefundAmount),
            PendingCount = transactions.Count(t => t.RefundStatus == RefundStatus.RefundRequested),
            ProcessingCount = transactions.Count(t => t.RefundStatus == RefundStatus.RefundProcessing),
            CompletedCount = transactions.Count(t => t.RefundStatus == RefundStatus.Refunded || t.RefundStatus == RefundStatus.Simulated),
            FailedCount = transactions.Count(t => t.RefundStatus == RefundStatus.RefundFailed)
        };
    }

    public async Task<AdminRefundDetailDto?> GetRefundDetailAsync(int paymentTransactionId)
    {
        var pt = await _unitOfWork.Repository<PaymentTransaction>().Query()
            .Include(t => t.Appointment).ThenInclude(a => a.Patient)
            .Include(t => t.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(t => t.Id == paymentTransactionId);

        if (pt == null) return null;

        return new AdminRefundDetailDto
        {
            PaymentTransactionId = pt.Id,
            AppointmentId = pt.AppointmentId,
            PatientName = pt.Appointment.Patient != null ? $"{pt.Appointment.Patient.FirstName} {pt.Appointment.Patient.LastName}" : "Unknown",
            PatientEmail = pt.Appointment.Patient?.Email ?? "",
            PatientPhone = pt.Appointment.Patient?.PhoneNumber,
            DoctorName = pt.Appointment.Doctor != null ? $"Dr. {pt.Appointment.Doctor.FirstName} {pt.Appointment.Doctor.LastName}" : "Unknown",
            DoctorSpecialty = pt.Appointment.Doctor?.Specialty ?? "",
            DoctorDepartment = pt.Appointment.Doctor?.Department?.Name,
            AppointmentDate = pt.Appointment.AppointmentDate,
            StartTime = pt.Appointment.StartTime.ToString(@"hh\:mm"),
            OriginalAmount = pt.GrossAmount > 0 ? pt.GrossAmount : pt.Amount,
            RefundAmount = pt.RefundAmount,
            PaymentStatus = pt.Status,
            RefundStatus = pt.RefundStatus,
            RazorpayPaymentId = pt.RazorpayPaymentId,
            RazorpayRefundId = pt.RazorpayRefundId,
            CancellationReason = pt.Appointment.CancellationReason,
            CancelledBy = pt.Appointment.CancelledBy,
            CancelledAt = pt.Appointment.CancelledAt,
            RefundInitiatedAt = pt.RefundInitiatedAt,
            RefundCompletedAt = pt.RefundCompletedAt,
            RefundFailureReason = pt.RefundFailureReason,
            RefundDestination = "Original Payment Method",
            AdminCommission = pt.AdminCommission,
            DoctorEarnings = pt.DoctorEarnings,
            NetDoctorAmount = pt.NetDoctorAmount,
            CreatedAt = pt.CreatedAt
        };
    }

    public async Task<bool> RetryRefundAsync(int paymentTransactionId)
    {
        var pt = await _unitOfWork.Repository<PaymentTransaction>().GetByIdAsync(paymentTransactionId);
        if (pt == null) throw new KeyNotFoundException($"PaymentTransaction {paymentTransactionId} not found.");

        if (string.IsNullOrEmpty(pt.RazorpayPaymentId))
        {
            throw new InvalidOperationException("Cannot refund a transaction with no RazorpayPaymentId.");
        }

        decimal refundableAmount = (pt.GrossAmount > 0 ? pt.GrossAmount : pt.Amount) - pt.RefundAmount;
        if (refundableAmount <= 0)
        {
            throw new InvalidOperationException("Transaction has already been fully refunded.");
        }

        try
        {
            pt.RefundStatus = RefundStatus.RefundRequested;
            pt.RefundInitiatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(pt);
            await _unitOfWork.SaveChangesAsync();

            var refundResult = await _paymentService.InitiateRefundAsync(
                pt.RazorpayPaymentId,
                refundableAmount,
                "Admin initiated retry refund",
                $"rcpt_{pt.AppointmentId}_{DateTime.UtcNow.Ticks}");

            if (refundResult.Success)
            {
                bool isComplete = refundResult.Status == RefundStatus.Refunded || refundResult.Status == RefundStatus.Simulated;
                pt.RefundStatus = isComplete ? RefundStatus.Refunded : RefundStatus.RefundProcessing;
                pt.RefundAmount += refundableAmount;
                pt.RazorpayRefundId = refundResult.RefundId;
                pt.RefundCompletedAt = isComplete ? DateTime.UtcNow : null;
                pt.RefundFailureReason = null;
            }
            else
            {
                pt.RefundStatus = RefundStatus.RefundFailed;
                pt.RefundFailureReason = refundResult.Message;
            }

            await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(pt);
            await _unitOfWork.SaveChangesAsync();
            return refundResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry refund for PaymentTransactionId: {Id}", paymentTransactionId);
            pt.RefundStatus = RefundStatus.RefundFailed;
            pt.RefundFailureReason = ex.Message;
            await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(pt);
            await _unitOfWork.SaveChangesAsync();
            return false;
        }
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
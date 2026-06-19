using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        INotificationService notificationService,
        ILogger<DoctorService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PagedResult<DoctorDto>> GetDoctorsAsync(
        int page, 
        int pageSize, 
        string? specialty = null, 
        int? departmentId = null, 
        string? search = null,
        string? gender = null,
        string? location = null,
        string? language = null,
        decimal? minFee = null,
        decimal? maxFee = null,
        decimal? minRating = null,
        bool? isAvailable = null)
    {
        var query = _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .Where(d => d.IsActive && d.ApprovalStatus == DoctorStatus.Approved);

        if (!string.IsNullOrWhiteSpace(specialty))
            query = query.Where(d => d.Specialty.Contains(specialty));

        if (departmentId.HasValue)
            query = query.Where(d => d.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.FirstName.Contains(search) || d.LastName.Contains(search) || d.Specialty.Contains(search));

        if (!string.IsNullOrWhiteSpace(gender))
            query = query.Where(d => d.Gender == gender);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(d => d.Location.Contains(location));

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(d => d.LanguagesSpoken.Contains(language));

        if (minFee.HasValue)
            query = query.Where(d => d.ConsultationFee >= minFee.Value);

        if (maxFee.HasValue)
            query = query.Where(d => d.ConsultationFee <= maxFee.Value);

        if (minRating.HasValue)
            query = query.Where(d => d.AverageRating >= minRating.Value);

        if (isAvailable.HasValue)
            query = query.Where(d => d.IsAvailable == isAvailable.Value);

        var total = await query.CountAsync();
        
        var doctors = await query
            .OrderByDescending(d => d.AverageRating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DoctorDto>
        {
            Items = doctors.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DoctorDto?> GetDoctorByIdAsync(int id)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive && d.ApprovalStatus == DoctorStatus.Approved);
        return doctor == null ? null : MapToDto(doctor);
    }

    public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        if ((await userRepo.FindAsync(u => u.Email == dto.Email)).Any())
            throw new InvalidOperationException("Email already exists.");

        var doctor = new Doctor
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            Specialty = dto.Specialty,
            Qualification = dto.Qualification,
            ExperienceYears = dto.ExperienceYears,
            ConsultationFee = dto.ConsultationFee,
            Bio = dto.Bio,
            DepartmentId = dto.DepartmentId
        };

        await _unitOfWork.Repository<Doctor>().AddAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Doctor,
            ReferenceId = doctor.Id,
            RefreshToken = string.Empty,
            RefreshTokenExpiry = DateTime.MinValue
        };
        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Doctor created: {Email}", dto.Email);
        return (await GetDoctorByIdAsync(doctor.Id))!;
    }

    public async Task<DoctorDto> UpdateDoctorAsync(int id, UpdateDoctorDto dto)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");

        doctor.FirstName = dto.FirstName;
        doctor.LastName = dto.LastName;
        doctor.PhoneNumber = dto.PhoneNumber;
        doctor.Specialty = dto.Specialty;
        doctor.Qualification = dto.Qualification;
        doctor.ExperienceYears = dto.ExperienceYears;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.Bio = dto.Bio;
        doctor.Gender = dto.Gender;
        doctor.Location = dto.Location;
        doctor.LanguagesSpoken = dto.LanguagesSpoken;
        doctor.IsAvailable = dto.IsAvailable;
        doctor.DepartmentId = dto.DepartmentId;
        doctor.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
        return (await GetDoctorByIdAsync(id))!;
    }

    public async Task DeleteDoctorAsync(int id)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor {id} not found.");
        doctor.IsActive = false;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<string> UploadProfileImageAsync(int doctorId, Stream imageStream, string fileName)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");
        var url = await _fileStorage.UploadAsync(imageStream, fileName, "doctors/profiles");
        doctor.ProfileImageUrl = url;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
        return url;
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

    public async Task UpdateScheduleAsync(int doctorId, IEnumerable<DoctorScheduleDto> schedules)
    {
        var scheduleRepo = _unitOfWork.Repository<DoctorSchedule>();
        var existing = await scheduleRepo.FindAsync(s => s.DoctorId == doctorId);
        
        foreach (var s in existing)
        {
            await scheduleRepo.DeleteAsync(s);
        }

        foreach (var s in schedules)
        {
            await scheduleRepo.AddAsync(new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SlotDurationMinutes = s.SlotDurationMinutes,
                IsActive = s.IsActive
            });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task BlockSlotAsync(int doctorId, BlockSlotDto dto)
    {
        var patientRepo = _unitOfWork.Repository<Patient>();
        var patient = await patientRepo.Query().FirstOrDefaultAsync();
        if (patient == null)
            throw new InvalidOperationException("No patient exists to assign this system block.");

        var apptRepo = _unitOfWork.Repository<Appointment>();
        var block = new Appointment
        {
            DoctorId = doctorId,
            PatientId = patient.Id,
            AppointmentDate = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.StartTime.Add(TimeSpan.FromMinutes(30)),
            Status = AppointmentStatus.Confirmed,
            Reason = $"Blocked: {dto.Reason}",
            Notes = "Doctor reserved slot",
            IsFollowUp = false,
            Fee = 0.00m
        };
        
        await apptRepo.AddAsync(block);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetVacationAsync(int doctorId, VacationDto dto)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException("Doctor not found.");

        doctor.IsAvailable = false;
        doctor.Bio = $"{doctor.Bio} (On Vacation from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}: {dto.Reason})";

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<DoctorEarningsDto> GetDoctorEarningsAsync(int doctorId)
    {
        var transactions = await _unitOfWork.Repository<PaymentTransaction>().Query()
            .Include(t => t.Appointment)
            .Where(t => t.Appointment.DoctorId == doctorId && t.Status == "Success")
            .ToListAsync();

        return new DoctorEarningsDto
        {
            TotalGrossEarnings = transactions.Sum(t => t.GrossAmount),
            TotalNetEarnings = transactions.Sum(t => t.NetDoctorAmount),
            TotalPlatformFeesPaid = transactions.Sum(t => t.PlatformFee),
            TotalTaxesPaid = transactions.Sum(t => t.TaxAmount),
            TotalAdminCommissionPaid = transactions.Sum(t => t.AdminCommission),
            PaidAppointmentsCount = transactions.Count
        };
    }

    public Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId)
        => _notificationService.GetNotificationsAsync(userId);

    public Task MarkNotificationAsReadAsync(int notificationId, int userId)
        => _notificationService.MarkAsReadAsync(notificationId, userId);

    public Task MarkAllNotificationsAsReadAsync(int userId)
        => _notificationService.MarkAllAsReadAsync(userId);
}


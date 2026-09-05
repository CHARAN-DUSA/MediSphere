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
    private readonly IAppUrlSettings _appUrlSettings;
    private readonly ICacheService _cacheService;

    public DoctorService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        INotificationService notificationService,
        ILogger<DoctorService> logger,
        IAppUrlSettings appUrlSettings,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _notificationService = notificationService;
        _logger = logger;
        _appUrlSettings = appUrlSettings;
        _cacheService = cacheService;
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
        var cacheKey = $"medisphere:doctors:list:p{page}_ps{pageSize}_sp{specialty}_dep{departmentId}_s{search}_g{gender}_loc{location}_lang{language}_minf{minFee}_maxf{maxFee}_minr{minRating}_av{isAvailable}";
        
        var cached = await _cacheService.GetAsync<PagedResult<DoctorDto>>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

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

        var result = new PagedResult<DoctorDto>
        {
            Items = doctors.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));
        return result;
    }

    public async Task<DoctorDto?> GetDoctorByIdAsync(int id)
    {
        var cacheKey = $"medisphere:doctor:{id}";
        var cached = await _cacheService.GetAsync<DoctorDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var doctor = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive && d.ApprovalStatus == DoctorStatus.Approved);
        
        if (doctor == null) return null;
        var dto = MapToDto(doctor);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
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

        await InvalidateDoctorCachesAsync(doctor.Id);

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

        await InvalidateDoctorCachesAsync(id);

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

        await InvalidateDoctorCachesAsync(id);
    }

    public async Task<(byte[] Data, string ContentType)?> GetProfileImageAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().Query()
            .Where(d => d.Id == doctorId && d.IsActive)
            .Select(d => new { d.ProfileImageData, d.ProfileImageContentType })
            .FirstOrDefaultAsync();

        if (doctor == null || doctor.ProfileImageData == null || doctor.ProfileImageData.Length == 0)
        {
            return null;
        }

        var contentType = string.IsNullOrWhiteSpace(doctor.ProfileImageContentType)
            ? "image/jpeg"
            : doctor.ProfileImageContentType;

        return (doctor.ProfileImageData, contentType);
    }

    public async Task<string> UploadProfileImageAsync(int doctorId, Stream imageStream, string contentType, string fileName)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        doctor.ProfileImageData = bytes;
        doctor.ProfileImageContentType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        doctor.ProfileImageUrl = $"/api/doctors/{doctorId}/profile-image";
        doctor.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateDoctorCachesAsync(doctorId);

        return FormatProfileImageUrl(doctorId, doctor.ProfileImageUrl, true);
    }

    private string FormatProfileImageUrl(int doctorId, string existingUrl, bool hasData)
    {
        var rawUrl = existingUrl;
        if (string.IsNullOrWhiteSpace(rawUrl) && hasData)
        {
            rawUrl = $"/api/doctors/{doctorId}/profile-image";
        }

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return rawUrl;
        }

        var baseUrl = _appUrlSettings.AppBaseUrl?.TrimEnd('/') ?? "";
        if (!rawUrl.StartsWith("/"))
        {
            rawUrl = "/" + rawUrl;
        }

        return string.IsNullOrEmpty(baseUrl) ? rawUrl : $"{baseUrl}{rawUrl}";
    }

    private DoctorDto MapToDto(Doctor d) => new()
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
        ProfileImageUrl = FormatProfileImageUrl(d.Id, d.ProfileImageUrl, d.ProfileImageData != null && d.ProfileImageData.Length > 0),
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
        await InvalidateDoctorCachesAsync(doctorId);

        try
        {
            await _cacheService.RemoveByPrefixAsync($"slots:{doctorId}:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot caches for Doctor {DoctorId}.",
                doctorId);
        }
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

        await InvalidateDoctorCachesAsync(doctorId);

        try
        {
            await _cacheService.RemoveAsync(
                $"slots:{doctorId}:{dto.Date:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot cache for Doctor {DoctorId} on {Date}.",
                doctorId,
                dto.Date.Date);
        }
    }

    public async Task SetVacationAsync(int doctorId, VacationDto dto)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException("Doctor not found.");

        doctor.IsAvailable = false;
        doctor.Bio = $"{doctor.Bio} (On Vacation from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}: {dto.Reason})";

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
        await InvalidateDoctorCachesAsync(doctorId);

        try
        {
            await _cacheService.RemoveByPrefixAsync($"slots:{doctorId}:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot caches for Doctor {DoctorId}.",
                doctorId);
        }
    }

    private async Task InvalidateDoctorCachesAsync(int? doctorId = null)
    {
        try
        {
            if (doctorId.HasValue)
            {
                await _cacheService.RemoveAsync($"medisphere:doctor:{doctorId.Value}");
            }
            await _cacheService.RemoveByPrefixAsync("medisphere:doctors:list:");
            await _cacheService.RemoveByPrefixAsync("medisphere:smartrecommend:");
            await _cacheService.RemoveByPrefixAsync("medisphere:home:");
            await _cacheService.RemoveAsync("medisphere:admin:dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate doctor caches.");
        }
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

    public async Task<IEnumerable<DailyScheduleSlotDto>> GetDailyScheduleAsync(
    int doctorId,
    DateTime date)
{
    var schedule = await _unitOfWork.Repository<DoctorSchedule>()
        .Query()
        .AsNoTracking()
        .FirstOrDefaultAsync(s =>
            s.DoctorId == doctorId &&
            s.DayOfWeek == date.DayOfWeek &&
            s.IsActive);

    if (schedule == null)
        return Enumerable.Empty<DailyScheduleSlotDto>();

    var appointments = await _unitOfWork.Repository<Appointment>()
        .Query()
        .AsNoTracking()
        .Include(a => a.Patient)
        .Where(a =>
            a.DoctorId == doctorId &&
            a.AppointmentDate.Date == date.Date &&
            a.Status != AppointmentStatus.Cancelled)
        .ToListAsync();

    var result = new List<DailyScheduleSlotDto>();

    var current = schedule.StartTime;

    while (current.Add(
        TimeSpan.FromMinutes(schedule.SlotDurationMinutes)
    ) <= schedule.EndTime)
    {
        var slotEnd =
            current.Add(
                TimeSpan.FromMinutes(
                    schedule.SlotDurationMinutes
                )
            );

        var appointment = appointments.FirstOrDefault(a =>
            a.StartTime < slotEnd &&
            a.EndTime > current);

        if (appointment == null)
        {
            result.Add(new DailyScheduleSlotDto
            {
                Date = date.Date,
                StartTime = current,
                EndTime = slotEnd,
                Status = "Available"
            });
        }
        else if (
            appointment.Reason != null &&
            appointment.Reason.StartsWith(
                "Blocked:",
                StringComparison.OrdinalIgnoreCase))
        {
            result.Add(new DailyScheduleSlotDto
            {
                Date = date.Date,
                StartTime = current,
                EndTime = slotEnd,
                Status = "Blocked",
                Reason = appointment.Reason.Substring("Blocked:".Length).Trim(),
                AppointmentId = appointment.Id
            });
        }
        else
        {
            result.Add(new DailyScheduleSlotDto
            {
                Date = date.Date,
                StartTime = current,
                EndTime = slotEnd,
                Status = "Booked",
                PatientName =
                    $"{appointment.Patient?.FirstName} {appointment.Patient?.LastName}".Trim(),
                AppointmentId = appointment.Id,
                Reason = appointment.Reason
            });
        }

        current = slotEnd;
    }

    return result;
}

public async Task DeleteBlockedSlotAsync(
    int doctorId,
    int appointmentId)
{
    var appointment =
        await _unitOfWork.Repository<Appointment>()
            .GetByIdAsync(appointmentId)
        ?? throw new KeyNotFoundException(
            "Blocked slot not found.");

    if (appointment.DoctorId != doctorId)
        throw new UnauthorizedAccessException(
            "This slot does not belong to the doctor.");

    if (
        appointment.Reason == null ||
        !appointment.Reason.StartsWith(
            "Blocked:",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Only blocked slots can be deleted.");
    }

    await _unitOfWork.Repository<Appointment>()
        .DeleteAsync(appointment);

    await _unitOfWork.SaveChangesAsync();

    await InvalidateDoctorCachesAsync(doctorId);

    try
    {
        await _cacheService.RemoveAsync(
            $"slots:{doctorId}:{appointment.AppointmentDate:yyyyMMdd}");
    }
    catch (Exception ex)
    {
        _logger.LogWarning(
            ex,
            "Unable to invalidate slot cache for Doctor {DoctorId} on {Date}.",
            doctorId,
            appointment.AppointmentDate.Date);
    }
}
    public Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId)
        => _notificationService.GetNotificationsAsync(userId);

    public Task MarkNotificationAsReadAsync(int notificationId, int userId)
        => _notificationService.MarkAsReadAsync(notificationId, userId);

    public Task MarkAllNotificationsAsReadAsync(int userId)
        => _notificationService.MarkAllAsReadAsync(userId);
}
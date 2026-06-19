using MediSphere.Application.Common;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly INotificationService _notificationService;
    private readonly IEmailSmsService _emailSms;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IUnitOfWork unitOfWork,
        ICacheService cache,
        INotificationService notificationService,
        IEmailSmsService emailSms,
        ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _notificationService = notificationService;
        _emailSms = emailSms;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // GET
    // ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<AppointmentDto>> GetAppointmentsAsync(
        int page,
        int pageSize,
        int? patientId = null,
        int? doctorId = null,
        string? status = null)
    {
        IQueryable<Appointment> query = _unitOfWork.Repository<Appointment>()
            .Query()
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Department);

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatus))
            query = query.Where(a => a.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        sw.Stop();
        _logger.LogInformation("Appointment query took {Ms} ms", sw.ElapsedMilliseconds);

        return new PagedResult<AppointmentDto>
        {
            Items = appointments.Select(MapToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        var appointment = await _unitOfWork.Repository<Appointment>().Query()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
                .ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(x => x.Id == id);

        return appointment == null ? null : MapToDto(appointment);
    }

    // ─────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────

    public async Task<AppointmentDto> CreateAppointmentAsync(int patientId, CreateAppointmentDto dto)
    {
        var lockKey = $"appointment:lock:{dto.DoctorId}:{dto.AppointmentDate:yyyyMMdd}:{dto.StartTime}";

        try
        {
            if (await _cache.ExistsAsync(lockKey))
                throw new InvalidOperationException("This slot is temporarily reserved. Please try another slot.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Continuing without distributed lock.");
        }

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId)
            ?? throw new KeyNotFoundException("Doctor not found.");

        var conflict = await _unitOfWork.Repository<Appointment>().Query()
            .AnyAsync(a =>
                a.DoctorId == dto.DoctorId &&
                a.AppointmentDate.Date == dto.AppointmentDate.Date &&
                a.StartTime == dto.StartTime &&
                a.Status != AppointmentStatus.Cancelled);

        if (conflict)
            throw new InvalidOperationException("This slot is already booked.");

        try
        {
            await _cache.SetAsync(lockKey, true, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Lock cache skipped.");
        }

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = dto.DoctorId,
            AppointmentDate = dto.AppointmentDate,
            StartTime = dto.StartTime,
            EndTime = dto.StartTime.Add(TimeSpan.FromMinutes(30)),
            Reason = dto.Reason,
            IsFollowUp = dto.IsFollowUp,
            PreviousAppointmentId = dto.PreviousAppointmentId,
            Fee = doctor.ConsultationFee,
            Status = AppointmentStatus.Pending
        };

        await _unitOfWork.Repository<Appointment>().AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        try { await _cache.RemoveAsync($"appointments:1:50:{patientId}::"); } catch { }

        _logger.LogInformation(
            "Appointment created: Patient {PatientId} with Doctor {DoctorId}", patientId, dto.DoctorId);

        return (await GetAppointmentByIdAsync(appointment.Id))!;
    }

    // ─────────────────────────────────────────────────────────────
    // UPDATE (reschedule)
    // ─────────────────────────────────────────────────────────────

    public async Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentDto dto)
    {
        var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Cannot update a completed or cancelled appointment.");

        appointment.AppointmentDate = dto.AppointmentDate;
        appointment.StartTime = dto.StartTime;
        appointment.EndTime = dto.StartTime.Add(TimeSpan.FromMinutes(30));
        appointment.Reason = dto.Reason;
        appointment.Status = AppointmentStatus.Rescheduled;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        try { await _cache.RemoveAsync("*"); } catch { }

        var updated = await _unitOfWork.Repository<Appointment>().Query()
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (updated?.Patient != null)
        {
            var patientName = $"{updated.Patient.FirstName} {updated.Patient.LastName}";
            var doctorName  = $"Dr. {updated.Doctor?.FirstName} {updated.Doctor?.LastName}";
            var newDate     = updated.AppointmentDate.ToString("dd MMM yyyy");
            var newTime     = updated.StartTime.ToString(@"hh\:mm");
            var specialty   = updated.Doctor?.Specialty ?? string.Empty;

            // — SignalR notifications —
            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Patient, updated.PatientId,
                "Appointment Rescheduled",
                $"Your appointment with {doctorName} has been rescheduled to {newDate} at {newTime}.",
                "Booking");

            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Doctor, updated.DoctorId,
                "Appointment Rescheduled",
                $"Appointment with {patientName} has been rescheduled.",
                "Booking");

            // — Emails —
            if (!string.IsNullOrEmpty(updated.Patient.Email))
            {
                var html = EmailTemplates.BuildAppointmentRescheduledEmail(
                    patientName, $"{updated.Doctor?.FirstName} {updated.Doctor?.LastName}",
                    specialty, newDate, newTime, updated.QueueToken);
                await _emailSms.SendEmailAsync(
                    updated.Patient.Email,
                    "Your MediSphere Appointment Has Been Rescheduled",
                    html);
            }

            if (!string.IsNullOrEmpty(updated.Doctor?.Email))
            {
                var html = EmailTemplates.BuildDoctorAppointmentRescheduledEmail(
                    $"{updated.Doctor.FirstName} {updated.Doctor.LastName}",
                    patientName, newDate, newTime);
                await _emailSms.SendEmailAsync(
                    updated.Doctor.Email,
                    "An Appointment Has Been Rescheduled — MediSphere",
                    html);
            }
        }

        return (await GetAppointmentByIdAsync(id))!;
    }

    // ─────────────────────────────────────────────────────────────
    // CANCEL
    // ─────────────────────────────────────────────────────────────

    public async Task CancelAppointmentAsync(int id, int requestingUserId, string role)
    {
        var appointment = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (role == "Patient" && appointment.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You cannot cancel this appointment.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed appointment.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        try { await _cache.RemoveAsync($"appointments:1:50:{appointment.PatientId}::"); } catch { }

        if (appointment.Patient != null)
        {
            var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
            var doctorName  = appointment.Doctor != null
                ? $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                : "your doctor";
            var date = appointment.AppointmentDate.ToString("dd MMM yyyy");
            var time = appointment.StartTime.ToString(@"hh\:mm");

            // — SignalR notifications —
            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Doctor, appointment.DoctorId,
                "Appointment Cancelled",
                $"Appointment with {patientName} was cancelled.",
                "Booking");

            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Patient, appointment.PatientId,
                "Appointment Cancelled",
                $"Your appointment with {doctorName} was cancelled.",
                "Booking");

            // — Emails —
            if (!string.IsNullOrEmpty(appointment.Patient.Email))
            {
                var html = EmailTemplates.BuildAppointmentCancelledEmail(
                    patientName,
                    appointment.Doctor != null
                        ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                        : "Doctor",
                    date, time, null);
                await _emailSms.SendEmailAsync(
                    appointment.Patient.Email,
                    "Your MediSphere Appointment Has Been Cancelled",
                    html);
            }

            if (!string.IsNullOrEmpty(appointment.Doctor?.Email))
            {
                var html = EmailTemplates.BuildDoctorAppointmentCancelledEmail(
                    $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                    patientName, date, time);
                await _emailSms.SendEmailAsync(
                    appointment.Doctor.Email,
                    "An Appointment Was Cancelled — MediSphere",
                    html);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // UPDATE STATUS
    // ─────────────────────────────────────────────────────────────

    public async Task<AppointmentDto> UpdateStatusAsync(int id, string status, string? notes = null)
    {
        _logger.LogWarning(
            "UpdateStatusAsync CALLED. AppointmentId={AppointmentId}, Status={Status}", id, status);

        var appointment = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (!Enum.TryParse<AppointmentStatus>(status, true, out var newStatus))
            throw new ArgumentException("Invalid status.");

        appointment.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(notes))
            appointment.Notes = notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        try { await _cache.RemoveAsync($"appointments:1:50:{appointment.PatientId}::"); } catch { }

        if (appointment.Patient != null)
        {
            var patientName   = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
            var doctorFullName = appointment.Doctor != null
                ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                : "Doctor";
            var doctorName    = $"Dr. {doctorFullName}";
            var specialty     = appointment.Doctor?.Specialty ?? string.Empty;
            var date          = appointment.AppointmentDate.ToString("dd MMM yyyy");
            var time          = appointment.StartTime.ToString(@"hh\:mm");
            var patientEmail  = appointment.Patient.Email;
            var doctorEmail   = appointment.Doctor?.Email;

            string title;
            string patientMessage;
            string doctorMessage;
            string type;

            switch (newStatus)
            {
                case AppointmentStatus.Pending:
                    title          = "Appointment Booked";
                    patientMessage = $"Your appointment with {doctorName} has been booked successfully.";
                    doctorMessage  = $"New appointment booked by {patientName}.";
                    type           = "Booking";
                    break;

                case AppointmentStatus.PendingPayment:
                    title          = "Payment Pending";
                    patientMessage = "Your appointment is waiting for payment confirmation.";
                    doctorMessage  = $"Appointment for {patientName} is awaiting payment.";
                    type           = "Payment";
                    break;

                case AppointmentStatus.Confirmed:
                    title          = "Appointment Confirmed";
                    patientMessage = $"Your appointment with {doctorName} has been confirmed.";
                    doctorMessage  = $"Appointment with {patientName} has been confirmed.";
                    type           = "Booking";

                    // Email — patient confirmation
                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var html = EmailTemplates.BuildAppointmentConfirmationEmail(
                            patientName, doctorFullName, specialty,
                            date, time, appointment.QueueToken,
                            appointment.Fee, appointment.MeetingUrl ?? string.Empty);
                        await _emailSms.SendEmailAsync(
                            patientEmail,
                            "Your MediSphere Appointment is Confirmed",
                            html);
                    }

                    // Email — doctor new appointment
                    if (!string.IsNullOrEmpty(doctorEmail))
                    {
                        var html = EmailTemplates.BuildDoctorNewAppointmentEmail(
                            doctorFullName, patientName, date, time,
                            appointment.QueueToken,
                            appointment.Reason ?? string.Empty,
                            appointment.Fee);
                        await _emailSms.SendEmailAsync(
                            doctorEmail,
                            "New Appointment Booked — MediSphere",
                            html);
                    }
                    break;

                case AppointmentStatus.Rescheduled:
                    title          = "Appointment Rescheduled";
                    patientMessage = $"Your appointment with {doctorName} has been rescheduled.";
                    doctorMessage  = $"Appointment with {patientName} has been rescheduled.";
                    type           = "Booking";
                    break;

                case AppointmentStatus.Completed:
                    title          = "Consultation Completed";
                    patientMessage = $"Your consultation with {doctorName} has been completed successfully.";
                    doctorMessage  = $"Consultation with {patientName} completed successfully.";
                    type           = "Consultation";

                    // Email — patient completed
                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var html = EmailTemplates.BuildAppointmentCompletedEmail(
                            patientName, doctorFullName, specialty, date, time, appointment.Fee);
                        await _emailSms.SendEmailAsync(
                            patientEmail,
                            "Your MediSphere Consultation is Complete",
                            html);
                    }

                    // Email — doctor completed + earnings
                    if (!string.IsNullOrEmpty(doctorEmail))
                    {
                        var doctorEarnings = appointment.Fee * 0.65m; // 65% doctor payout
                        var html = EmailTemplates.BuildDoctorAppointmentCompletedEmail(
                            doctorFullName, patientName, date, time, doctorEarnings);
                        await _emailSms.SendEmailAsync(
                            doctorEmail,
                            "Consultation Completed — MediSphere",
                            html);
                    }
                    break;

                case AppointmentStatus.Cancelled:
                    title          = "Appointment Cancelled";
                    patientMessage = $"Your appointment with {doctorName} has been cancelled.";
                    doctorMessage  = $"Appointment with {patientName} has been cancelled.";
                    type           = "Booking";

                    // Email — patient cancelled
                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var html = EmailTemplates.BuildAppointmentCancelledEmail(
                            patientName, doctorFullName, date, time, notes);
                        await _emailSms.SendEmailAsync(
                            patientEmail,
                            "Your MediSphere Appointment Has Been Cancelled",
                            html);
                    }

                    // Email — doctor cancelled
                    if (!string.IsNullOrEmpty(doctorEmail))
                    {
                        var html = EmailTemplates.BuildDoctorAppointmentCancelledEmail(
                            doctorFullName, patientName, date, time);
                        await _emailSms.SendEmailAsync(
                            doctorEmail,
                            "An Appointment Was Cancelled — MediSphere",
                            html);
                    }
                    break;
                    
                case AppointmentStatus.NoShow:
                    title          = "Appointment Missed";
                    patientMessage = "You were marked as No Show because the appointment time passed.";
                    doctorMessage  = $"{patientName} did not attend the appointment.";
                    type           = "Booking";

                    // Email — patient no show
                    if (!string.IsNullOrEmpty(patientEmail))
                    {
                        var html = EmailTemplates.BuildPatientNoShowEmail(
                            patientName, doctorFullName, date, time);
                        await _emailSms.SendEmailAsync(
                            patientEmail,
                            "You Missed Your MediSphere Appointment",
                            html);
                    }

                    // Email — doctor no show alert
                    if (!string.IsNullOrEmpty(doctorEmail))
                    {
                        var html = EmailTemplates.BuildDoctorNoShowAlertEmail(
                            doctorFullName, patientName, date, time);
                        await _emailSms.SendEmailAsync(
                            doctorEmail,
                            "Patient Did Not Attend — MediSphere",
                            html);
                    }
                    break;

                default:
                    title          = "Appointment Updated";
                    patientMessage = $"Your appointment status changed to {newStatus}.";
                    doctorMessage  = $"Appointment status changed to {newStatus}.";
                    type           = "Booking";
                    break;
            }

            // — SignalR notifications (always fire for every status) —
            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Patient, appointment.PatientId, title, patientMessage, type);

            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Doctor, appointment.DoctorId, title, doctorMessage, type);
        }

        _logger.LogInformation(
            "Appointment {AppointmentId} status changed to {Status}", appointment.Id, newStatus);

        return (await GetAppointmentByIdAsync(id))!;
    }

    // ─────────────────────────────────────────────────────────────
    // AVAILABLE SLOTS
    // ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<TimeSpan>> GetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        var cacheKey = $"slots:{doctorId}:{date:yyyyMMdd}";

        try
        {
            var cached = await _cache.GetAsync<List<TimeSpan>>(cacheKey);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Continuing without cache.");
        }

        var schedule = await _unitOfWork.Repository<DoctorSchedule>().Query()
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId &&
                s.DayOfWeek == date.DayOfWeek &&
                s.IsActive);

        if (schedule == null)
            return Enumerable.Empty<TimeSpan>();

        var booked = await _unitOfWork.Repository<Appointment>().Query()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == date.Date &&
                a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.StartTime)
            .ToListAsync();

        var slots   = new List<TimeSpan>();
        var current = schedule.StartTime;

        while (current.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes)) <= schedule.EndTime)
        {
            if (!booked.Contains(current))
                slots.Add(current);
            current = current.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes));
        }

        try
        {
            await _cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Cache write skipped.");
        }

        return slots;
    }

    // ─────────────────────────────────────────────────────────────
    // MAP
    // ─────────────────────────────────────────────────────────────

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id                    = a.Id,
        PatientId             = a.PatientId,
        PatientName           = $"{a.Patient?.FirstName} {a.Patient?.LastName}",
        DoctorId              = a.DoctorId,
        DoctorName            = $"Dr. {a.Doctor?.FirstName} {a.Doctor?.LastName}",
        DepartmentName        = a.Doctor?.Department?.Name ?? string.Empty,
        AppointmentDate       = a.AppointmentDate,
        StartTime             = a.StartTime,
        EndTime               = a.EndTime,
        Status                = a.Status.ToString(),
        Reason                = a.Reason,
        Notes                 = a.Notes,
        IsFollowUp            = a.IsFollowUp,
        Fee                   = a.Fee,
        TelemedicineMeetingId = a.TelemedicineMeetingId,
        MeetingUrl            = a.MeetingUrl,
        QueueToken            = a.QueueToken,
        QueueStatus           = a.QueueStatus,
        PaymentStatus         = a.PaymentStatus,
        RazorpayOrderId       = a.RazorpayOrderId,
        CreatedAt             = a.CreatedAt
    };
}

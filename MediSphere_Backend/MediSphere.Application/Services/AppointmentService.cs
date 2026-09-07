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
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IUnitOfWork unitOfWork,
        ICacheService cache,
        INotificationService notificationService,
        IEmailSmsService emailSms,
        IPaymentService paymentService,
        ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _notificationService = notificationService;
        _emailSms = emailSms;
        _paymentService = paymentService;
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
            .Where(a => !a.IsBlockedSlot) // Never return doctor-blocked system slots in patient listings
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Department)
            .Include(a => a.PaymentTransactions);

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatus))
            query = query.Where(a => a.Status == parsedStatus);

        var totalCount = await query.CountAsync();

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var reviewedIds = new HashSet<int>(
            await _unitOfWork.Repository<DoctorReview>().Query()
                .Where(r => appointmentIds.Contains(r.AppointmentId))
                .Select(r => r.AppointmentId)
                .ToListAsync());

        return new PagedResult<AppointmentDto>
        {
            Items = appointments.Select(a => MapToDto(a, reviewedIds.Contains(a.Id))),
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
            .Include(x => x.PaymentTransactions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment == null) return null;

        var hasReviewed = await _unitOfWork.Repository<DoctorReview>().Query()
            .AnyAsync(r => r.AppointmentId == id);

        return MapToDto(appointment, hasReviewed);
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

        try
        {
            await _cache.RemoveAsync(
                $"slots:{dto.DoctorId}:{dto.AppointmentDate:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot cache for Doctor {DoctorId} on {Date}.",
                dto.DoctorId,
                dto.AppointmentDate.Date);
        }

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
            var doctorName = $"Dr. {updated.Doctor?.FirstName} {updated.Doctor?.LastName}";
            var newDate = updated.AppointmentDate.ToString("dd MMM yyyy");
            var newTime = updated.StartTime.ToString(@"hh\:mm");
            var specialty = updated.Doctor?.Specialty ?? string.Empty;

            // — SignalR notifications —
            if (updated.PatientId.HasValue)
            {
                await _notificationService.SendToRoleReferenceAsync(
                    UserRole.Patient, updated.PatientId.Value,
                    "Appointment Rescheduled",
                    $"Your appointment with {doctorName} has been rescheduled to {newDate} at {newTime}.",
                    "Booking");
            }

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

    public async Task CancelAppointmentAsync(int id, int requestingUserId, string role, CancelAppointmentDto? cancelDto = null)
    {
        var appointment = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.PaymentTransactions)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("Appointment not found.");

        if (role == "Patient" && appointment.PatientId != requestingUserId)
            throw new UnauthorizedAccessException("You cannot cancel this appointment.");

        if (role == "Doctor" && appointment.DoctorId != requestingUserId)
            throw new UnauthorizedAccessException("You cannot cancel an appointment for another doctor.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed appointment.");

        PaymentTransaction? transaction = appointment.PaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault(t => t.Status == "Success");

        bool isAlreadyCancelled = appointment.Status == AppointmentStatus.Cancelled;

        if (isAlreadyCancelled)
        {
            if (transaction == null ||
                transaction.RefundStatus == RefundStatus.Refunded ||
                transaction.RefundStatus == RefundStatus.Simulated ||
                (transaction.GrossAmount - transaction.RefundAmount <= 0))
            {
                _logger.LogInformation("Appointment {Id} is already cancelled and refund is finalized. Idempotent skip.", id);
                return;
            }

            _logger.LogInformation("Appointment {Id} is already cancelled, but refund status is {RefundStatus}. Attempting refund recovery...", id, transaction.RefundStatus);
        }

        // 1. Audit State
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt ??= DateTime.UtcNow;
        appointment.CancelledBy = role;
        appointment.CancelledByUserId = requestingUserId;
        appointment.CancellationReasonType = cancelDto?.ReasonType ?? appointment.CancellationReasonType ?? CancellationReasonType.PatientRequest;
        appointment.CancellationReason = !string.IsNullOrWhiteSpace(cancelDto?.Reason) 
            ? cancelDto.Reason 
            : (!string.IsNullOrWhiteSpace(appointment.CancellationReason) ? appointment.CancellationReason : "No reason specified");
        appointment.UpdatedAt = DateTime.UtcNow;

        // 2. Automated Financial Refund Lifecycle
        string? refundId = null;
        decimal? refundedAmount = null;
        string? refundStatusString = null;

        if (transaction != null && transaction.RefundStatus != RefundStatus.Refunded && transaction.RefundStatus != RefundStatus.Simulated)
        {
            var refundableAmount = transaction.GrossAmount - transaction.RefundAmount;
            if (refundableAmount > 0)
            {
                var refundResult = await _paymentService.InitiateRefundAsync(
                    transaction.RazorpayPaymentId,
                    refundableAmount,
                    $"Cancelled by {role}: {appointment.CancellationReason}");

                if (refundResult.Success)
                {
                    transaction.RefundStatus = refundResult.Status;
                    transaction.RefundAmount += refundResult.Amount;
                    transaction.RazorpayRefundId = refundResult.RefundId;
                    transaction.RefundInitiatedAt = DateTime.UtcNow;
                    if (refundResult.Status == RefundStatus.Refunded || refundResult.Status == RefundStatus.Simulated)
                    {
                        transaction.RefundCompletedAt = DateTime.UtcNow;
                        appointment.PaymentStatus = "Refunded";
                    }
                    else if (refundResult.Status == RefundStatus.RefundProcessing)
                    {
                        transaction.RefundStatus = RefundStatus.RefundProcessing;
                        appointment.PaymentStatus = "Refund Processing";
                    }
                    else
                    {
                        transaction.RefundStatus = RefundStatus.RefundRequested;
                        appointment.PaymentStatus = "Refund Initiated";
                    }

                    refundId = refundResult.RefundId;
                    refundedAmount = refundResult.Amount;
                    refundStatusString = refundResult.Status.ToString();

                    await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(transaction);
                }
                else
                {
                    transaction.RefundStatus = RefundStatus.RefundFailed;
                    transaction.RefundFailureReason = refundResult.Message;
                    appointment.PaymentStatus = "Refund Failed";
                    await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(transaction);
                }
            }
        }
        else if (appointment.PaymentStatus != "Refunded" && appointment.PaymentStatus != "Paid")
        {
            appointment.PaymentStatus = "Cancelled";
        }

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        if (appointment.PatientId.HasValue)
        {
            try { await _cache.RemoveAsync($"appointments:1:50:{appointment.PatientId.Value}::"); } catch { }
        }

        try
        {
            await _cache.RemoveAsync(
                $"slots:{appointment.DoctorId}:{appointment.AppointmentDate:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot cache for Doctor {DoctorId} on {Date}.",
                appointment.DoctorId,
                appointment.AppointmentDate.Date);
        }

        if (appointment.Patient != null)
        {
            var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
            var doctorName = appointment.Doctor != null
                ? $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                : "your doctor";
            var date = appointment.AppointmentDate.ToString("dd MMM yyyy");
            var time = appointment.StartTime.ToString(@"hh\:mm");

            // — SignalR notifications —
            await _notificationService.SendToRoleReferenceAsync(
                UserRole.Doctor, appointment.DoctorId,
                "Appointment Cancelled",
                $"Appointment with {patientName} on {date} at {time} was cancelled by {role}.",
                "Booking");

            if (appointment.PatientId.HasValue)
            {
                await _notificationService.SendToRoleReferenceAsync(
                    UserRole.Patient, appointment.PatientId.Value,
                    "Appointment Cancelled",
                    $"Your appointment with {doctorName} on {date} was cancelled.",
                    "Booking");
            }

            // — Emails —
            if (!string.IsNullOrEmpty(appointment.Patient.Email))
            {
                var html = EmailTemplates.BuildAppointmentCancelledWithRefundEmail(
                    patientName,
                    appointment.Doctor != null
                        ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                        : "Doctor",
                    date, time, appointment.CancellationReason,
                    refundedAmount, refundId, refundStatusString);

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
            throw new ArgumentException("Invalid status value.");

        appointment.Status = newStatus;
        if (!string.IsNullOrEmpty(notes))
            appointment.Notes = notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        if (appointment.PatientId.HasValue)
        {
            try { await _cache.RemoveAsync($"appointments:1:50:{appointment.PatientId.Value}::"); } catch { }
        }

        try
        {
            await _cache.RemoveAsync(
                $"slots:{appointment.DoctorId}:{appointment.AppointmentDate:yyyyMMdd}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to invalidate slot cache for Doctor {DoctorId} on {Date}.",
                appointment.DoctorId,
                appointment.AppointmentDate.Date);
        }

        var updated = await _unitOfWork.Repository<Appointment>().Query()
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (updated?.Patient != null)
        {
            var patientName = $"{updated.Patient.FirstName} {updated.Patient.LastName}";
            var doctorName = $"Dr. {updated.Doctor?.FirstName} {updated.Doctor?.LastName}";
            var date = updated.AppointmentDate.ToString("dd MMM yyyy");
            var time = updated.StartTime.ToString(@"hh\:mm");

            if (newStatus == AppointmentStatus.Completed)
            {
                if (updated.PatientId.HasValue)
                {
                    await _notificationService.SendToRoleReferenceAsync(
                        UserRole.Patient, updated.PatientId.Value,
                        "Consultation Completed",
                        $"Your consultation with {doctorName} is complete. You can now leave a review.",
                        "Booking");
                }

                if (!string.IsNullOrEmpty(updated.Doctor?.Email))
                {
                    var transaction = await _unitOfWork.Repository<PaymentTransaction>().Query()
                        .FirstOrDefaultAsync(t => t.AppointmentId == id && t.Status == "Success");
                    var earnings = transaction?.NetDoctorAmount ?? 0m;

                    var html = EmailTemplates.BuildDoctorAppointmentCompletedEmail(
                        $"{updated.Doctor.FirstName} {updated.Doctor.LastName}",
                        patientName, date, time, earnings);
                    await _emailSms.SendEmailAsync(
                        updated.Doctor.Email,
                        "Consultation Completed — Earnings Credited",
                        html);
                }
            }
            else if (newStatus == AppointmentStatus.NoShow)
            {
                if (updated.PatientId.HasValue)
                {
                    await _notificationService.SendToRoleReferenceAsync(
                        UserRole.Patient, updated.PatientId.Value,
                        "Appointment Missed",
                        $"You missed your scheduled appointment with {doctorName}.",
                        "Booking");
                }

                if (!string.IsNullOrEmpty(updated.Patient.Email))
                {
                    var html = EmailTemplates.BuildPatientNoShowEmail(
                        patientName, $"{updated.Doctor?.FirstName} {updated.Doctor?.LastName}",
                        date, time);
                    await _emailSms.SendEmailAsync(
                        updated.Patient.Email,
                        "Missed Appointment — MediSphere",
                        html);
                }
            }
        }

        return (await GetAppointmentByIdAsync(id))!;
    }

    // ─────────────────────────────────────────────────────────────
    // AVAILABLE SLOTS
    // ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<AppointmentSlotDto>> GetAvailableSlotsAsync(
        int doctorId,
        DateTime date)
    {
        var cacheKey = $"slots:{doctorId}:{date:yyyyMMdd}";

        try
        {
            var cached = await _cache.GetAsync<List<AppointmentSlotDto>>(cacheKey);
            if (cached != null) return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Slot cache read skipped.");
        }

        var schedule = await _unitOfWork.Repository<DoctorSchedule>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId &&
                s.DayOfWeek == date.DayOfWeek &&
                s.IsActive);

        if (schedule == null) return Enumerable.Empty<AppointmentSlotDto>();

        var appointments = await _unitOfWork.Repository<Appointment>()
            .Query()
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == date.Date &&
                a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        var slots = new List<AppointmentSlotDto>();
        var current = schedule.StartTime;

        while (current.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes)) <= schedule.EndTime)
        {
            var endTime = current.Add(TimeSpan.FromMinutes(schedule.SlotDurationMinutes));
            string status;

            var appointment = appointments.FirstOrDefault(a => a.StartTime == current);

            if (appointment == null)
            {
                status = "Available";
            }
            else if (appointment.IsBlockedSlot ||
                (!string.IsNullOrWhiteSpace(appointment.Reason) &&
                 appointment.Reason.StartsWith("Blocked:", StringComparison.OrdinalIgnoreCase)))
            {
                status = "Blocked";
            }
            else
            {
                status = "Booked";
            }

            slots.Add(new AppointmentSlotDto
            {
                StartTime = current,
                EndTime = endTime,
                Status = status
            });

            current = endTime;
        }

        try
        {
            await _cache.SetAsync(cacheKey, slots, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable. Slot cache write skipped.");
        }

        return slots;
    }

    // ─────────────────────────────────────────────────────────────
    // MAP
    // ─────────────────────────────────────────────────────────────

    private static AppointmentDto MapToDto(Appointment a, bool hasReviewed = false)
    {
        var latestTx = a.PaymentTransactions?.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

        return new AppointmentDto
        {
            Id = a.Id,
            HasReviewed = hasReviewed,
            PatientId = a.PatientId,
            PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : (a.IsBlockedSlot ? "Doctor Blocked Slot" : "Unassigned"),
            DoctorId = a.DoctorId,
            DoctorName = $"Dr. {a.Doctor?.FirstName} {a.Doctor?.LastName}",
            DepartmentName = a.Doctor?.Department?.Name ?? string.Empty,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status.ToString(),
            Reason = a.Reason,
            Notes = a.Notes,
            IsFollowUp = a.IsFollowUp,
            Fee = a.Fee,
            TelemedicineMeetingId = a.TelemedicineMeetingId,
            MeetingUrl = a.MeetingUrl,
            QueueToken = a.QueueToken,
            QueueStatus = a.QueueStatus,
            PaymentStatus = a.PaymentStatus,
            RazorpayOrderId = a.RazorpayOrderId,
            CreatedAt = a.CreatedAt,
            IsBlockedSlot = a.IsBlockedSlot,
            CancelledAt = a.CancelledAt,
            CancelledBy = a.CancelledBy,
            CancelledByUserId = a.CancelledByUserId,
            CancellationReasonType = a.CancellationReasonType,
            CancellationReason = a.CancellationReason,
            RefundAmount = latestTx?.RefundAmount ?? 0m,
            RefundStatus = latestTx?.RefundStatus.ToString(),
            RazorpayRefundId = latestTx?.RazorpayRefundId
        };
    }
}
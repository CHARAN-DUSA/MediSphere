using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.Common;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

public class AccountDeletionService : IAccountDeletionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEmailSmsService _emailSms;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEmailSmsService emailSms,
        IPaymentService paymentService,
        ILogger<AccountDeletionService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _emailSms = emailSms;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task DeletePatientAccountAsync(int patientId, string reason = "User requested account deletion")
    {
        var patient = await _unitOfWork.Repository<Patient>().Query()
            .Include(p => p.FamilyMembers)
            .FirstOrDefaultAsync(p => p.Id == patientId)
            ?? throw new KeyNotFoundException($"Patient with id {patientId} not found.");

        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.Role == UserRole.Patient && u.ReferenceId == patientId);

        var originalEmail = patient.Email;
        var originalName = $"{patient.FirstName} {patient.LastName}".Trim();
        var anonymizedEmail = $"deleted_patient_{patientId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}@medisphere.invalid";

        // 1. Resolve upcoming appointments and auto-refund
        var upcomingAppointments = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Doctor)
            .Include(a => a.PaymentTransactions)
            .Where(a => a.PatientId == patientId &&
                       (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.PendingPayment) &&
                       a.AppointmentDate >= DateTime.UtcNow.Date)
            .ToListAsync();

        foreach (var appt in upcomingAppointments)
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancelledBy = "Patient (Account Deletion)";
            appt.CancellationReason = $"Patient closed account. Reason: {reason}";
            appt.CancelledAt = DateTime.UtcNow;

            var transaction = appt.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
            if (transaction != null && appt.PaymentStatus == "Paid")
            {
                appt.PaymentStatus = "Refunded";
                transaction.RefundStatus = RefundStatus.RefundProcessing;
                transaction.RefundAmount = transaction.Amount;

                if (!string.IsNullOrEmpty(transaction.RazorpayPaymentId))
                {
                    try
                    {
                        var refundRes = await _paymentService.InitiateRefundAsync(
                            transaction.RazorpayPaymentId,
                            transaction.Amount,
                            $"Account deletion refund for appointment #{appt.Id}");

                        if (refundRes.Success)
                        {
                            transaction.RefundStatus = refundRes.Status;
                            transaction.RazorpayRefundId = refundRes.RefundId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-refund appointment #{Id} during patient deletion", appt.Id);
                    }
                }
                await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(transaction);
            }

            await _unitOfWork.Repository<Appointment>().UpdateAsync(appt);

            // Notify Doctor
            if (appt.Doctor != null && !string.IsNullOrEmpty(appt.Doctor.Email))
            {
                try
                {
                    var docCancelEmail = EmailTemplates.BuildDoctorAppointmentCancelledEmail(
                        $"{appt.Doctor.FirstName} {appt.Doctor.LastName}",
                        originalName,
                        appt.AppointmentDate.ToString("yyyy-MM-dd"),
                        appt.StartTime.ToString(@"hh\:mm"),
                        $"Patient closed account: {reason}",
                        "Patient (Account Deletion)");
                    await _emailSms.SendEmailAsync(appt.Doctor.Email, "Appointment Cancelled (Patient Account Closed)", docCancelEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation email to doctor {DocId}", appt.DoctorId);
                }
            }
        }

        // 2. Soft-delete and anonymize Patient entity
        patient.IsActive = false;
        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;
        patient.IsAnonymized = true;
        patient.AnonymizedAt = DateTime.UtcNow;
        patient.Email = anonymizedEmail;
        patient.FirstName = "Anonymized";
        patient.LastName = $"Patient #{patientId}";
        patient.PhoneNumber = string.Empty;
        patient.Address = string.Empty;
        patient.MedicalHistory = string.Empty;

        // 3. Anonymize auth user & revoke refresh tokens
        if (user != null)
        {
            user.IsActive = false;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.Email = anonymizedEmail;
            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiry = DateTime.MinValue;
            await _unitOfWork.Repository<AppUser>().UpdateAsync(user);
        }

        await _unitOfWork.Repository<Patient>().UpdateAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        // 4. Send Confirmation & Admin Alert Emails
        if (!string.IsNullOrEmpty(originalEmail) && !originalEmail.EndsWith(".invalid"))
        {
            try
            {
                var patientDelEmail = EmailTemplates.BuildAccountDeletedEmail(originalName, "Patient", DateTime.UtcNow);
                await _emailSms.SendEmailAsync(originalEmail, "MediSphere Account Deactivation Confirmation", patientDelEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send account deletion email to {Email}", originalEmail);
            }
        }

        try
        {
            var adminAlertEmail = EmailTemplates.BuildPatientAccountDeletedAdminAlertEmail("Admin", originalName, originalEmail, reason);
            await _emailSms.SendEmailAsync("admin@medisphere.com", "Patient Account Deletion Alert", adminAlertEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send patient deletion alert to admin");
        }

        // 5. Invalidate caches
        try
        {
            await _cacheService.RemoveAsync($"appointments:1:50:{patientId}::");
            await _cacheService.RemoveAsync("medisphere:admin:dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache after deleting patient {Id}", patientId);
        }

        _logger.LogInformation("Patient account {Id} successfully soft-deleted and anonymized. Reason: {Reason}", patientId, reason);
    }

    public async Task DeleteDoctorAccountAsync(int doctorId, string reason = "User requested account deletion")
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor with id {doctorId} not found.");

        var user = await _unitOfWork.Repository<AppUser>().Query()
            .FirstOrDefaultAsync(u => u.Role == UserRole.Doctor && u.ReferenceId == doctorId);

        var originalEmail = doctor.Email;
        var originalName = $"{doctor.FirstName} {doctor.LastName}".Trim();
        var specialty = doctor.Specialty;
        var anonymizedEmail = $"deleted_doc_{doctorId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}@medisphere.invalid";

        // 1. Resolve upcoming appointments and auto-refund patients
        var upcomingAppointments = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Patient)
            .Include(a => a.PaymentTransactions)
            .Where(a => a.DoctorId == doctorId &&
                       (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.PendingPayment) &&
                       a.AppointmentDate >= DateTime.UtcNow.Date)
            .ToListAsync();

        foreach (var appt in upcomingAppointments)
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancelledBy = "System (Doctor Deactivation)";
            appt.CancellationReason = $"Doctor profile deactivated. Reason: {reason}";
            appt.CancelledAt = DateTime.UtcNow;

            var transaction = appt.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
            decimal? refundedAmount = null;
            string? refundId = null;
            string? refundStatus = null;

            if (transaction != null && appt.PaymentStatus == "Paid")
            {
                appt.PaymentStatus = "Refunded";
                transaction.RefundStatus = RefundStatus.RefundProcessing;
                transaction.RefundAmount = transaction.Amount;
                refundedAmount = transaction.Amount;
                refundStatus = "RefundProcessing";

                if (!string.IsNullOrEmpty(transaction.RazorpayPaymentId))
                {
                    try
                    {
                        var refundRes = await _paymentService.InitiateRefundAsync(
                            transaction.RazorpayPaymentId,
                            transaction.Amount,
                            $"Doctor deactivation refund for appointment #{appt.Id}");

                        if (refundRes.Success)
                        {
                            transaction.RefundStatus = refundRes.Status;
                            transaction.RazorpayRefundId = refundRes.RefundId;
                            refundId = refundRes.RefundId;
                            refundStatus = refundRes.Status.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-refund appointment #{Id} during doctor deletion", appt.Id);
                    }
                }
                await _unitOfWork.Repository<PaymentTransaction>().UpdateAsync(transaction);
            }

            await _unitOfWork.Repository<Appointment>().UpdateAsync(appt);

            // Notify Patient
            if (appt.Patient != null && !string.IsNullOrEmpty(appt.Patient.Email))
            {
                try
                {
                    var patCancelEmail = EmailTemplates.BuildAppointmentCancelledWithRefundEmail(
                        $"{appt.Patient.FirstName} {appt.Patient.LastName}",
                        originalName,
                        appt.AppointmentDate.ToString("yyyy-MM-dd"),
                        appt.StartTime.ToString(@"hh\:mm"),
                        "Doctor profile deactivated on MediSphere",
                        refundedAmount,
                        refundId,
                        refundStatus);
                    await _emailSms.SendEmailAsync(appt.Patient.Email, "Appointment Cancelled & Refund Initiated", patCancelEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send cancellation email to patient {PatId}", appt.PatientId);
                }
            }
        }

        // 2. Soft-delete and deactivate Doctor entity
        doctor.IsActive = false;
        doctor.IsAvailable = false;
        doctor.IsDeleted = true;
        doctor.DeletedAt = DateTime.UtcNow;
        doctor.Email = anonymizedEmail;
        doctor.PhoneNumber = string.Empty;

        // 3. Anonymize auth user & revoke refresh tokens
        if (user != null)
        {
            user.IsActive = false;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.Email = anonymizedEmail;
            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiry = DateTime.MinValue;
            await _unitOfWork.Repository<AppUser>().UpdateAsync(user);
        }

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        // 4. Send Confirmation & Admin Alert Emails
        if (!string.IsNullOrEmpty(originalEmail) && !originalEmail.EndsWith(".invalid"))
        {
            try
            {
                var docDelEmail = EmailTemplates.BuildAccountDeletedEmail($"Dr. {originalName}", "Doctor", DateTime.UtcNow);
                await _emailSms.SendEmailAsync(originalEmail, "MediSphere Doctor Profile Deactivation Confirmation", docDelEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send account deletion email to doctor {Email}", originalEmail);
            }
        }

        try
        {
            var adminAlertEmail = EmailTemplates.BuildDoctorAccountDeletedAdminAlertEmail("Admin", originalName, originalEmail, specialty, reason);
            await _emailSms.SendEmailAsync("admin@medisphere.com", "Doctor Profile Deactivation Alert", adminAlertEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send doctor deletion alert to admin");
        }

        // 5. Invalidate caches
        try
        {
            await _cacheService.RemoveAsync($"medisphere:doctor:{doctorId}");
            await _cacheService.RemoveByPrefixAsync("medisphere:doctors:list:");
            await _cacheService.RemoveAsync("medisphere:admin:dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache after deleting doctor {Id}", doctorId);
        }

        _logger.LogInformation("Doctor account {Id} successfully soft-deleted. Reason: {Reason}", doctorId, reason);
    }
}

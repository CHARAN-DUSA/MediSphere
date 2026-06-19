using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueueController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _signalRService;
    private readonly ILogger<QueueController> _logger;
    private readonly INotificationService _notificationService;
    public QueueController(
    IUnitOfWork unitOfWork,
    ISignalRNotificationService signalRService,
    ILogger<QueueController> logger,
    INotificationService notificationService)
{
    _unitOfWork = unitOfWork;
    _signalRService = signalRService;
    _logger = logger;
    _notificationService = notificationService;
}

    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AppointmentDto>>>> GetDoctorQueue(int doctorId)
    {
        try
        {
            var appointments = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department)
                .Where(a => a.DoctorId == doctorId &&
                             a.AppointmentDate.Date == DateTime.Today &&
                             a.Status != AppointmentStatus.Cancelled &&
                             a.Status != AppointmentStatus.Pending && // Skip unpaid/pending
                             a.QueueStatus != "Completed")
                .OrderBy(a => a.QueueToken)
                .ToListAsync();

            var dtos = appointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = $"{a.Patient?.FirstName} {a.Patient?.LastName}",
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
                CreatedAt = a.CreatedAt
            });

            return Ok(ApiResponse<IEnumerable<AppointmentDto>>.Ok(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load doctor queue.");
            return StatusCode(500, ApiResponse<IEnumerable<AppointmentDto>>.Fail($"Queue error: {ex.Message}"));
        }
    }

    [HttpPost("call-next/{doctorId}")]
    [Authorize(Roles = "Doctor,Receptionist,Admin")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> CallNextPatient(int doctorId)
    {
        try
        {
            // 1. Mark any active "InConsultation" appointment for today as "Completed"
            var activeConsultations = await _unitOfWork.Repository<Appointment>().Query()
                .Where(a => a.DoctorId == doctorId &&
                             a.AppointmentDate.Date == DateTime.Today &&
                             a.QueueStatus == "InConsultation")
                .ToListAsync();

            foreach (var act in activeConsultations)
            {
                act.QueueStatus = "Completed";
                act.Status = AppointmentStatus.Completed;
                await _unitOfWork.Repository<Appointment>().UpdateAsync(act);
            }

            // 2. Fetch the next waiting patient in the queue
            var nextAppointment = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department)
                .Where(a => a.DoctorId == doctorId &&
                             a.AppointmentDate.Date == DateTime.Today &&
                             a.Status != AppointmentStatus.Cancelled &&
                             a.Status != AppointmentStatus.Pending && // Skip unpaid/pending
                             a.QueueStatus == "Waiting")
                .OrderBy(a => a.QueueToken)
                .FirstOrDefaultAsync();

            if (nextAppointment == null)
            {
                await _unitOfWork.SaveChangesAsync();

                // Broadcast updates to notify screen that queue is empty
                var doc = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId);
                if (doc != null)
                {
                    await _signalRService.NotifyQueueUpdateAsync(doctorId, doc.DepartmentId);
                }

                return Ok(ApiResponse<AppointmentDto>.Ok(null!, "No more patients waiting in queue."));
            }

            // 3. Mark next patient as InConsultation
            nextAppointment.QueueStatus = "InConsultation";
            await _unitOfWork.Repository<Appointment>().UpdateAsync(nextAppointment);
            await _unitOfWork.SaveChangesAsync();

            // 4. Trigger SignalR broadcasts to update Patient dashboard tickers and Clinic congestion monitors
            await _signalRService.NotifyQueueUpdateAsync(doctorId, nextAppointment.Doctor.DepartmentId);
            await _signalRService.NotifyQueuePositionChangedAsync(nextAppointment.PatientId, nextAppointment.QueueToken, "InConsultation");
            await _signalRService.NotifyConsultationStatusChangedAsync(nextAppointment.Id, "InConsultation");

            var dto = new AppointmentDto
            {
                Id = nextAppointment.Id,
                PatientId = nextAppointment.PatientId,
                PatientName = $"{nextAppointment.Patient?.FirstName} {nextAppointment.Patient?.LastName}",
                DoctorId = nextAppointment.DoctorId,
                DoctorName = $"Dr. {nextAppointment.Doctor?.FirstName} {nextAppointment.Doctor?.LastName}",
                DepartmentName = nextAppointment.Doctor?.Department?.Name ?? string.Empty,
                AppointmentDate = nextAppointment.AppointmentDate,
                StartTime = nextAppointment.StartTime,
                EndTime = nextAppointment.EndTime,
                Status = nextAppointment.Status.ToString(),
                Reason = nextAppointment.Reason,
                Notes = nextAppointment.Notes,
                IsFollowUp = nextAppointment.IsFollowUp,
                Fee = nextAppointment.Fee,
                TelemedicineMeetingId = nextAppointment.TelemedicineMeetingId,
                MeetingUrl = nextAppointment.MeetingUrl,
                QueueToken = nextAppointment.QueueToken,
                QueueStatus = nextAppointment.QueueStatus,
                PaymentStatus = nextAppointment.PaymentStatus,
                RazorpayOrderId = nextAppointment.RazorpayOrderId,
                CreatedAt = nextAppointment.CreatedAt
            };

            return Ok(ApiResponse<AppointmentDto>.Ok(dto, $"Called token #{dto.QueueToken} to consultation room."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call next patient.");
            return StatusCode(500, ApiResponse<AppointmentDto>.Fail($"Call next failed: {ex.Message}"));
        }
    }

    [HttpPost("update-status/{appointmentId}")]
    [Authorize(Roles = "Doctor,Receptionist,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateQueueStatus(int appointmentId, [FromBody] UpdateQueueStatusRequest req)
    {
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().Query()
    .Include(a => a.Patient)
    .Include(a => a.Doctor)
    .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound(ApiResponse<object>.Fail("Appointment not found."));
            }

            appointment.QueueStatus = req.QueueStatus;

            if (req.QueueStatus == "Completed")
            {
                appointment.Status = AppointmentStatus.Completed;
            }

            await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
            await _unitOfWork.SaveChangesAsync();
            if (req.QueueStatus == "Completed")
{
    await _notificationService.SendToRoleReferenceAsync(
        UserRole.Patient,
        appointment.PatientId,
        "Consultation Completed",
        $"Your consultation with Dr. {appointment.Doctor?.FirstName} {appointment.Doctor?.LastName} has been completed successfully.",
        "Consultation");

    await _notificationService.SendToRoleReferenceAsync(
        UserRole.Doctor,
        appointment.DoctorId,
        "Consultation Completed",
        $"Consultation with {appointment.Patient?.FirstName} {appointment.Patient?.LastName} completed successfully.",
        "Consultation");
}

            // Broadcast queue adjustments
            await _signalRService.NotifyQueueUpdateAsync(appointment.DoctorId, appointment.Doctor?.DepartmentId ?? 0);
            await _signalRService.NotifyQueuePositionChangedAsync(appointment.PatientId, appointment.QueueToken, req.QueueStatus);
            await _signalRService.NotifyConsultationStatusChangedAsync(appointment.Id, req.QueueStatus);

            return Ok(ApiResponse<object>.Ok(null!, "Queue status updated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update queue status.");
            return StatusCode(500, ApiResponse<object>.Fail($"Update queue status failed: {ex.Message}"));
        }
    }
}

public record UpdateQueueStatusRequest(string QueueStatus);

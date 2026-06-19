using System.Security.Claims;
using MediatR;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using MediSphere.Application.Features.Appointments.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IMediator _mediator;

    public AppointmentsController(IAppointmentService appointmentService, IMediator mediator)
    {
        _appointmentService = appointmentService;
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetAppointments(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? patientId = null, [FromQuery] int? doctorId = null, [FromQuery] string? status = null)
    {
        var result = await _appointmentService.GetAppointmentsAsync(page, pageSize, patientId, doctorId, status);
        return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(result));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetMyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _appointmentService.GetAppointmentsAsync(page, pageSize, patientId: refId);
        return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetAppointment(int id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<AppointmentDto>.Fail("Appointment not found."));
        return Ok(ApiResponse<AppointmentDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Patient,Receptionist")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> CreateAppointment([FromBody] CreateAppointmentDto dto)
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var command = new BookAppointmentCommand
        {
            PatientId = refId,
            DoctorId = dto.DoctorId,
            AppointmentDate = dto.AppointmentDate,
            StartTime = dto.StartTime,
            Reason = dto.Reason,
            IsFollowUp = dto.IsFollowUp,
            PreviousAppointmentId = dto.PreviousAppointmentId,
            UseRewardPoints = dto.UseRewardPoints,
            ReferralCode = dto.ReferralCode
        };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAppointment), new { id = result.Id }, ApiResponse<AppointmentDto>.Ok(result, "Appointment booked."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Patient,Receptionist")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto dto)
    {
        var result = await _appointmentService.UpdateAppointmentAsync(id, dto);
        return Ok(ApiResponse<AppointmentDto>.Ok(result, "Appointment rescheduled."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelAppointment(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        await _appointmentService.CancelAppointmentAsync(id, userId, role);
        return Ok(ApiResponse<object>.Ok(null!, "Appointment cancelled."));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Doctor,Admin,Receptionist")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var result = await _appointmentService.UpdateStatusAsync(id, req.Status, req.Notes);
        return Ok(ApiResponse<AppointmentDto>.Ok(result, "Status updated."));
    }

    [HttpGet("slots")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TimeSpan>>>> GetAvailableSlots([FromQuery] int doctorId, [FromQuery] DateTime date)
    {
        var slots = await _appointmentService.GetAvailableSlotsAsync(doctorId, date);
        return Ok(ApiResponse<IEnumerable<TimeSpan>>.Ok(slots));
    }

    
}

public record UpdateStatusRequest(string Status, string? Notes);

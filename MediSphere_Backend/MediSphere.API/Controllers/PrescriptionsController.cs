using System.Security.Claims;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Prescription;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _service;

    public PrescriptionsController(
        IPrescriptionService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Create(
        [FromBody] CreatePrescriptionDto dto)
    {
        var doctorId = GetReferenceId();

        var result = await _service.CreateAsync(
            doctorId,
            dto);

        return Ok(
            ApiResponse<PrescriptionDto>.Ok(
                result,
                "Prescription created successfully."));
    }

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<ActionResult<
        ApiResponse<IEnumerable<PrescriptionDto>>>> GetPatientHistory(
            int patientId)
    {
        var role = GetRole();

        if (role.Equals("Patient",
            StringComparison.OrdinalIgnoreCase))
        {
            var currentPatientId = GetReferenceId();

            if (currentPatientId != patientId)
                return Forbid();

            var patientHistory =
                await _service.GetPatientHistoryAsync(patientId);

            return Ok(
                ApiResponse<IEnumerable<PrescriptionDto>>
                    .Ok(patientHistory));
        }

        if (role.Equals("Doctor",
            StringComparison.OrdinalIgnoreCase))
        {
            var doctorId = GetReferenceId();

            var doctorHistory =
                await _service.GetDoctorPatientHistoryAsync(
                    doctorId,
                    patientId);

            return Ok(
                ApiResponse<IEnumerable<PrescriptionDto>>
                    .Ok(doctorHistory));
        }

        var history =
            await _service.GetPatientHistoryAsync(patientId);

        return Ok(
            ApiResponse<IEnumerable<PrescriptionDto>>
                .Ok(history));
    }

    [HttpGet("appointment/{appointmentId}")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>>
     GetByAppointment(int appointmentId)
    {
        try
        {
            var role = GetRole();
            var userId = GetReferenceId();

            var result =
                await _service.GetByAppointmentAsync(
                    appointmentId,
                    userId,
                    role);

            if (result == null)
                return NotFound(
                    ApiResponse<PrescriptionDto>
                        .Fail("Prescription not found."));

            return Ok(
                ApiResponse<PrescriptionDto>.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
    private int GetReferenceId()
    {
        var referenceId =
            User.FindFirst("referenceId")?.Value;

        if (int.TryParse(referenceId, out var id) && id > 0)
            return id;

        var nameIdentifier =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(nameIdentifier, out id) && id > 0)
            return id;

        throw new UnauthorizedAccessException(
            "User reference ID is missing.");
    }

    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value
            ?? string.Empty;
    }
}
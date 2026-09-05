using System.Security.Claims;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/doctor-consultations")]
[Authorize(Roles = "Doctor")]
public class DoctorConsultationsController : ControllerBase
{
    private readonly IDoctorConsultationService _service;

    public DoctorConsultationsController(
        IDoctorConsultationService service)
    {
        _service = service;
    }

    [HttpGet("{appointmentId:int}")]
    public async Task<IActionResult> GetConsultation(
        int appointmentId)
    {
        var refValue =
            User.FindFirst("referenceId")?.Value;

        if (!int.TryParse(refValue, out var doctorId) ||
            doctorId <= 0)
        {
            return Unauthorized();
        }

        try
        {
            var result =
                await _service.GetConsultationAsync(
                    doctorId,
                    appointmentId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = result,
                Message = "Consultation data loaded successfully."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}
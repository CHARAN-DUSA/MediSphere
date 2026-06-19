using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService) => _patientService = patientService;

    [HttpGet("profile")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientProfileDto>>> GetProfile()
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _patientService.GetProfileAsync(refId);
        if (result == null) return NotFound(ApiResponse<PatientProfileDto>.Fail("Patient profile not found."));
        return Ok(ApiResponse<PatientProfileDto>.Ok(result));
    }

    [HttpPut("profile")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientProfileDto>>> UpdateProfile([FromBody] PatientProfileDto dto)
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _patientService.UpdateProfileAsync(refId, dto);
        return Ok(ApiResponse<PatientProfileDto>.Ok(result, "Profile updated successfully."));
    }

    [HttpGet("favorites")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetFavorites()
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _patientService.GetFavoritesAsync(refId);
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.Ok(result));
    }

    [HttpPost("favorites/{doctorId}")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleFavorite(int doctorId)
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _patientService.ToggleFavoriteAsync(refId, doctorId);
        var msg = result ? "Doctor added to favorites." : "Doctor removed from favorites.";
        return Ok(ApiResponse<bool>.Ok(result, msg));
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetNotifications()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _patientService.GetNotificationsAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(result));
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkNotificationAsRead(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _patientService.MarkNotificationAsReadAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(null!, "Notification dismissed."));
    }

    [Authorize(Roles = "Patient")]
    [HttpPost("notifications/read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllNotificationsAsRead()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _patientService.MarkAllNotificationsAsReadAsync(userId);
        return Ok(ApiResponse<object>.Ok(null!, "All notifications dismissed."));
    }
[HttpPost("family-members")]
[Authorize(Roles = "Patient")]
public async Task<IActionResult> AddFamilyMember(
    [FromBody] FamilyMemberDto dto)
{
    var patientId = int.Parse(
        User.FindFirst("referenceId")?.Value ?? "0");

    var result = await _patientService
        .AddFamilyMemberAsync(patientId, dto);

    return Ok(result);
}

[HttpDelete("family-members/{id}")]
public async Task<IActionResult> DeleteFamilyMember(int id)
{
    await _patientService.DeleteFamilyMemberAsync(id);
    return NoContent();
}
}

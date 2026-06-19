using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService) => _doctorService = doctorService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorDto>>>> GetDoctors(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? specialty = null, 
        [FromQuery] int? departmentId = null, 
        [FromQuery] string? search = null,
        [FromQuery] string? gender = null,
        [FromQuery] string? location = null,
        [FromQuery] string? language = null,
        [FromQuery] decimal? minFee = null,
        [FromQuery] decimal? maxFee = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] bool? isAvailable = null)
    {
        var result = await _doctorService.GetDoctorsAsync(
            page, 
            pageSize, 
            specialty, 
            departmentId, 
            search, 
            gender, 
            location, 
            language, 
            minFee, 
            maxFee, 
            minRating, 
            isAvailable);
            
        return Ok(ApiResponse<PagedResult<DoctorDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetDoctor(int id)
    {
        var result = await _doctorService.GetDoctorByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<DoctorDto>.Fail("Doctor not found."));
        return Ok(ApiResponse<DoctorDto>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        var result = await _doctorService.CreateDoctorAsync(dto);
        return CreatedAtAction(nameof(GetDoctor), new { id = result.Id }, ApiResponse<DoctorDto>.Ok(result, "Doctor created."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> UpdateDoctor(int id, [FromBody] UpdateDoctorDto dto)
    {
        var result = await _doctorService.UpdateDoctorAsync(id, dto);
        return Ok(ApiResponse<DoctorDto>.Ok(result, "Doctor updated."));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDoctor(int id)
    {
        await _doctorService.DeleteDoctorAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor deleted."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id}/profile-image")]
    public async Task<ActionResult<ApiResponse<string>>> UploadProfileImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse<string>.Fail("No file provided."));
        using var stream = file.OpenReadStream();
        var url = await _doctorService.UploadProfileImageAsync(id, stream, file.FileName);
        return Ok(ApiResponse<string>.Ok(url, "Profile image uploaded."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id}/schedule")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSchedule(int id, [FromBody] IEnumerable<DoctorScheduleDto> schedules)
    {
        await _doctorService.UpdateScheduleAsync(id, schedules);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor schedule updated successfully."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id}/block-slot")]
    public async Task<ActionResult<ApiResponse<object>>> BlockSlot(int id, [FromBody] BlockSlotDto dto)
    {
        await _doctorService.BlockSlotAsync(id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Time slot blocked successfully."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id}/vacation")]
    public async Task<ActionResult<ApiResponse<object>>> SetVacation(int id, [FromBody] VacationDto dto)
    {
        await _doctorService.SetVacationAsync(id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Vacation period scheduled successfully."));
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id}/earnings")]
    public async Task<ActionResult<ApiResponse<DoctorEarningsDto>>> GetDoctorEarnings(int id)
    {
        var result = await _doctorService.GetDoctorEarningsAsync(id);
        return Ok(ApiResponse<DoctorEarningsDto>.Ok(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetNotifications()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _doctorService.GetNotificationsAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(result));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("notifications/{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkNotificationAsRead(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _doctorService.MarkNotificationAsReadAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(null!, "Notification dismissed."));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("notifications/read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllNotificationsAsRead()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        await _doctorService.MarkAllNotificationsAsReadAsync(userId);
        return Ok(ApiResponse<object>.Ok(null!, "All notifications dismissed."));
    }
}


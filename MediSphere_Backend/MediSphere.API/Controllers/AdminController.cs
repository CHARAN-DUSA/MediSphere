using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Admin;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Notification;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly INotificationService _notificationService;

    public AdminController(IAdminService adminService, INotificationService notificationService)
    {
        _adminService = adminService;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboard()
    {
        var result = await _adminService.GetDashboardStatsAsync();
        return Ok(ApiResponse<DashboardStatsDto>.Ok(result));
    }

    // =========================================================
    // DOCTOR MANAGEMENT ENDPOINTS
    // =========================================================
    [HttpPost("doctors/{id}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveDoctor(int id, [FromBody] ApproveDoctorRequest req)
    {
        await _adminService.ApproveDoctorAsync(id, req.Approve);
        var msg = req.Approve ? "Doctor profile approved." : "Doctor profile rejected.";
        return Ok(ApiResponse<object>.Ok(null!, msg));
    }

    [HttpPost("doctors/{id}/suspend")]
    public async Task<ActionResult<ApiResponse<object>>> SuspendDoctor(int id)
    {
        await _adminService.SuspendDoctorAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor profile suspended."));
    }

    [HttpPost("doctors/{id}/block")]
    public async Task<ActionResult<ApiResponse<object>>> BlockDoctor(int id)
    {
        await _adminService.BlockDoctorAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor profile blocked."));
    }

    [HttpPost("doctors/{id}/unblock")]
    public async Task<ActionResult<ApiResponse<object>>> UnblockDoctor(int id)
    {
        await _adminService.UnblockDoctorAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor profile unblocked."));
    }

    [HttpGet("doctors")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetDoctors()
    {
        var result = await _adminService.GetAllDoctorsForAdminAsync();
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.Ok(result));
    }

    [HttpGet("doctors/search")]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorDto>>>> SearchDoctors(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _adminService.SearchDoctorsAsync(query, page, pageSize);
        return Ok(ApiResponse<PagedResult<DoctorDto>>.Ok(result));
    }

    [HttpGet("doctors/{id}")]
    public async Task<ActionResult<ApiResponse<AdminDoctorDetailDto>>> GetDoctorDetail(int id)
    {
        var result = await _adminService.GetDoctorDetailAsync(id);
        if (result == null) return NotFound(ApiResponse<AdminDoctorDetailDto>.Fail("Doctor not found."));
        return Ok(ApiResponse<AdminDoctorDetailDto>.Ok(result));
    }

    [HttpDelete("doctors/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDoctor(int id, [FromQuery] string reason = "Admin initiated deletion")
    {
        await _adminService.DeleteDoctorAsync(id, reason);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor account soft-deleted and deactivated."));
    }

    // =========================================================
    // USER & PATIENT MANAGEMENT ENDPOINTS
    // =========================================================
    [HttpPost("users/block")]
    public async Task<ActionResult<ApiResponse<object>>> BlockUser([FromBody] BlockUserRequest req)
    {
        await _adminService.BlockUserAsync(req.Email, req.Block);
        var msg = req.Block ? "User blocked successfully." : "User unblocked successfully.";
        return Ok(ApiResponse<object>.Ok(null!, msg));
    }

    [HttpGet("patients")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatientProfileDto>>>> GetPatients()
    {
        var result = await _adminService.GetAllPatientsAsync();
        return Ok(ApiResponse<IEnumerable<PatientProfileDto>>.Ok(result));
    }

    [HttpGet("patients/search")]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientProfileDto>>>> SearchPatients(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _adminService.SearchPatientsAsync(query, page, pageSize);
        return Ok(ApiResponse<PagedResult<PatientProfileDto>>.Ok(result));
    }

    [HttpGet("patients/{id}")]
    public async Task<ActionResult<ApiResponse<AdminPatientDetailDto>>> GetPatientDetail(int id)
    {
        var result = await _adminService.GetPatientDetailAsync(id);
        if (result == null) return NotFound(ApiResponse<AdminPatientDetailDto>.Fail("Patient not found."));
        return Ok(ApiResponse<AdminPatientDetailDto>.Ok(result));
    }

    [HttpDelete("patients/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePatient(int id, [FromQuery] string reason = "Admin initiated deletion")
    {
        await _adminService.DeletePatientAsync(id, reason);
        return Ok(ApiResponse<object>.Ok(null!, "Patient account soft-deleted and anonymized."));
    }

    // =========================================================
    // REFUND MANAGEMENT ENDPOINTS
    // =========================================================
    [HttpGet("refunds")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminRefundDto>>>> GetRefunds(
        [FromQuery] string? query,
        [FromQuery] RefundStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new AdminRefundFilterDto
        {
            Query = query,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };
        var result = await _adminService.GetRefundsAsync(filter);
        return Ok(ApiResponse<PagedResult<AdminRefundDto>>.Ok(result));
    }

    [HttpGet("refunds/summary")]
    public async Task<ActionResult<ApiResponse<AdminRefundSummaryDto>>> GetRefundSummary()
    {
        var result = await _adminService.GetRefundSummaryAsync();
        return Ok(ApiResponse<AdminRefundSummaryDto>.Ok(result));
    }

    [HttpGet("refunds/{id}")]
    public async Task<ActionResult<ApiResponse<AdminRefundDetailDto>>> GetRefundDetail(int id)
    {
        var result = await _adminService.GetRefundDetailAsync(id);
        if (result == null) return NotFound(ApiResponse<AdminRefundDetailDto>.Fail("Refund transaction not found."));
        return Ok(ApiResponse<AdminRefundDetailDto>.Ok(result));
    }

    [HttpPost("refunds/{id}/retry")]
    public async Task<ActionResult<ApiResponse<object>>> RetryRefund(int id)
    {
        var success = await _adminService.RetryRefundAsync(id);
        if (success)
        {
            return Ok(ApiResponse<object>.Ok(null!, "Refund process initiated successfully."));
        }
        return BadRequest(ApiResponse<object>.Fail("Refund retry failed. Check failure details in the refund record."));
    }

    // =========================================================
    // SETTINGS & CONTENT ENDPOINTS
    // =========================================================
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingDto>>>> GetSettings()
    {
        var result = await _adminService.GetSystemSettingsAsync();
        return Ok(ApiResponse<IEnumerable<SystemSettingDto>>.Ok(result));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateSetting([FromBody] SystemSettingDto dto)
    {
        await _adminService.UpdateSystemSettingAsync(dto);
        return Ok(ApiResponse<object>.Ok(null!, "System setting updated successfully."));
    }

    [HttpGet("content")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContentItemDto>>>> GetContent([FromQuery] string? type = null)
    {
        var result = await _adminService.GetContentItemsAsync(type);
        return Ok(ApiResponse<IEnumerable<ContentItemDto>>.Ok(result));
    }

    [HttpPost("content")]
    public async Task<ActionResult<ApiResponse<ContentItemDto>>> UpsertContent([FromBody] ContentItemDto dto)
    {
        var result = await _adminService.UpsertContentItemAsync(dto);
        return Ok(ApiResponse<ContentItemDto>.Ok(result, "Content item updated successfully."));
    }

    [HttpDelete("content/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteContent(int id)
    {
        await _adminService.DeleteContentItemAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Content item deleted successfully."));
    }

    [HttpPost("broadcast")]
    public async Task<ActionResult<ApiResponse<object>>> Broadcast([FromBody] BroadcastDto dto)
    {
        await _notificationService.BroadcastNotificationAsync(dto);
        return Ok(ApiResponse<object>.Ok(null!, "Broadcast alert sent to all users."));
    }
}

public record ApproveDoctorRequest(bool Approve);
public record BlockUserRequest(string Email, bool Block);

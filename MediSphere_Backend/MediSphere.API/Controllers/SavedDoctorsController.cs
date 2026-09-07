using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Patient")]
public class SavedDoctorsController : ControllerBase
{
    private readonly ISavedDoctorService _savedDoctorService;

    public SavedDoctorsController(ISavedDoctorService savedDoctorService)
    {
        _savedDoctorService = savedDoctorService;
    }

    [HttpPost("{doctorId}")]
    public async Task<ActionResult<ApiResponse<object>>> SaveDoctor(int doctorId)
    {
        var patientId = GetPatientId();
        await _savedDoctorService.SaveDoctorAsync(patientId, doctorId);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor saved to favorites."));
    }

    [HttpDelete("{doctorId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveSavedDoctor(int doctorId)
    {
        var patientId = GetPatientId();
        await _savedDoctorService.RemoveSavedDoctorAsync(patientId, doctorId);
        return Ok(ApiResponse<object>.Ok(null!, "Doctor removed from favorites."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetSavedDoctors()
    {
        var patientId = GetPatientId();
        var result = await _savedDoctorService.GetSavedDoctorsAsync(patientId);
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.Ok(result));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> SearchSavedDoctors([FromQuery] string query)
    {
        var patientId = GetPatientId();
        var result = await _savedDoctorService.SearchSavedDoctorsAsync(patientId, query);
        return Ok(ApiResponse<IEnumerable<DoctorDto>>.Ok(result));
    }

    private int GetPatientId()
    {
        return int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
    }
}

using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.MedicalRecord;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _service;

    public MedicalRecordsController(IMedicalRecordService service) => _service = service;

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicalRecordDto>>>> GetPatientRecords(int patientId)
    {
        var result = await _service.GetPatientRecordsAsync(patientId);
        return Ok(ApiResponse<IEnumerable<MedicalRecordDto>>.Ok(result));
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<MedicalRecordDto>>> Upload(
        IFormFile file, [FromForm] int? appointmentId, [FromForm] string description = "")
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<MedicalRecordDto>.Fail("No file provided."));

        var patientId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        using var stream = file.OpenReadStream();
        var result = await _service.UploadRecordAsync(patientId, appointmentId, stream, file.FileName, description);
        return Ok(ApiResponse<MedicalRecordDto>.Ok(result, "Record uploaded."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var patientId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        await _service.DeleteRecordAsync(id, patientId);
        return Ok(ApiResponse<object>.Ok(null!, "Record deleted."));
    }
}

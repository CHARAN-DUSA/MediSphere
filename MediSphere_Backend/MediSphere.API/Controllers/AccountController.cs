using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountDeletionService _accountDeletionService;

    public AccountController(IAccountDeletionService accountDeletionService)
    {
        _accountDeletionService = accountDeletionService;
    }

    [HttpDelete("me")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMyAccount([FromBody] DeleteAccountRequest? req)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var refIdClaim = User.FindFirst("referenceId")?.Value;
        var reason = req?.Reason ?? "User self-requested account deletion";

        if (role == "Patient")
        {
            if (int.TryParse(refIdClaim, out var patientId) && patientId > 0)
            {
                await _accountDeletionService.DeletePatientAccountAsync(patientId, reason);
                return Ok(ApiResponse<object>.Ok(null!, "Your account has been deleted and personal data anonymized."));
            }
            return BadRequest(ApiResponse<object>.Fail("Patient profile reference not found."));
        }
        else if (role == "Doctor")
        {
            if (int.TryParse(refIdClaim, out var doctorId) && doctorId > 0)
            {
                await _accountDeletionService.DeleteDoctorAccountAsync(doctorId, reason);
                return Ok(ApiResponse<object>.Ok(null!, "Doctor profile has been deactivated."));
            }
            return BadRequest(ApiResponse<object>.Fail("Doctor profile reference not found."));
        }

        return BadRequest(ApiResponse<object>.Fail("Account deletion is only supported for Patients and Doctors."));
    }
}

public record DeleteAccountRequest(string? Reason);

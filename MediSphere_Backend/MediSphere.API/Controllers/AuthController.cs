using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Auth;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("strict")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterPatientDto dto)
    {
        var result = await _authService.RegisterPatientAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
    }

    [HttpPost("register/doctor")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterDoctor([FromBody] RegisterDoctorDto dto)
    {
        var result = await _authService.RegisterDoctorAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Doctor registration submitted for approval."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result));
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        await _authService.RevokeTokenAsync(email);
        return Ok(ApiResponse<object>.Ok(null!, "Token revoked."));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(ApiResponse<object>.Ok(null!, "If the email is registered, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(ApiResponse<object>.Ok(null!, "Password has been reset successfully."));
    }
}

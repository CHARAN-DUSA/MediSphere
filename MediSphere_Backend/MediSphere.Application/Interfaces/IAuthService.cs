using MediSphere.Application.DTOs.Auth;

namespace MediSphere.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto);
    Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);
    Task RevokeTokenAsync(string email);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}

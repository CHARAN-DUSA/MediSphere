using MediSphere.Domain.Entities;

namespace MediSphere.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    int? ValidateTokenAndGetUserId(string token);
}

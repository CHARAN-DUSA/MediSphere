using MediSphere.Domain.Common;
using MediSphere.Domain.Enums;

namespace MediSphere.Domain.Entities;

public class AppUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? ReferenceId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;
}

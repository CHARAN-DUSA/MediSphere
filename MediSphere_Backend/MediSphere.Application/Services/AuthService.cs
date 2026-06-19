using System.Security.Cryptography;
using MediSphere.Application.Common;
using MediSphere.Application.DTOs.Auth;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

public class AuthService : IAuthService
{
    private const int PasswordResetTokenExpiryMinutes = 10;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailSmsService _emailSms;
    private readonly IAppUrlSettings _appUrlSettings;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IEmailSmsService emailSms,
        IAppUrlSettings appUrlSettings,
        ICacheService cacheService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailSms = emailSms;
        _appUrlSettings = appUrlSettings;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var patientRepo = _unitOfWork.Repository<Patient>();

        var existingUser = (await userRepo.FindAsync(u => u.Email == dto.Email)).FirstOrDefault();
        if (existingUser != null)
            throw new InvalidOperationException("Email already registered.");

        string initials = dto.FirstName.Length >= 4
            ? dto.FirstName[..4].ToUpper()
            : dto.FirstName.ToUpper();
        string generatedReferralCode = $"{initials}{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        var patient = new Patient
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Address = dto.Address,
            BloodGroup = dto.BloodGroup,
            ReferredByCode = dto.ReferralCode,
            ReferralCode = generatedReferralCode,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        await patientRepo.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Patient,
            ReferenceId = patient.Id,
            RefreshToken = _jwtService.GenerateRefreshToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(user);
        _logger.LogInformation("Patient registered: {Email}", dto.Email);

        string patientRegEmailBody = EmailTemplates.BuildPatientRegistrationSuccessEmail(
            $"{patient.FirstName} {patient.LastName}", patient.ReferralCode);

        await _emailSms.SendEmailAsync(patient.Email, "Welcome to MediSphere!", patientRegEmailBody);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Role = user.Role.ToString(),
            Email = user.Email,
            UserId = user.Id,
            ReferenceId = user.ReferenceId ?? 0
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    var userRepo = _unitOfWork.Repository<AppUser>();

    var user = (await userRepo.FindAsync(
        u => u.Email == dto.Email && u.IsActive))
        .FirstOrDefault();

    _logger.LogInformation(
        "User query took {Ms} ms",
        sw.ElapsedMilliseconds);

    if (user == null)
        throw new UnauthorizedAccessException("Invalid email or password.");

    sw.Restart();

    var passwordValid = BCrypt.Net.BCrypt.Verify(
        dto.Password,
        user.PasswordHash);

    _logger.LogInformation(
        "BCrypt Verify took {Ms} ms",
        sw.ElapsedMilliseconds);

    if (!passwordValid)
        throw new UnauthorizedAccessException("Invalid email or password.");

    user.RefreshToken = _jwtService.GenerateRefreshToken();
    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

    await userRepo.UpdateAsync(user);
    await _unitOfWork.SaveChangesAsync();

    var accessToken = _jwtService.GenerateAccessToken(user);

    _logger.LogInformation(
        "User logged in: {Email}",
        user.Email);

    return new AuthResponseDto
    {
        AccessToken = accessToken,
        RefreshToken = user.RefreshToken,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        Role = user.Role.ToString(),
        Email = user.Email,
        UserId = user.Id,
        ReferenceId = user.ReferenceId ?? 0
    };
}
    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var user = (await userRepo.FindAsync(u => u.RefreshToken == dto.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow)).FirstOrDefault();

        if (user == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Role = user.Role.ToString(),
            Email = user.Email,
            UserId = user.Id,
            ReferenceId = user.ReferenceId ?? 0
        };
    }

    public async Task RevokeTokenAsync(string email)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var user = (await userRepo.FindAsync(u => u.Email == email)).FirstOrDefault();
        if (user == null) return;

        user.RefreshToken = string.Empty;
        user.RefreshTokenExpiry = DateTime.MinValue;
        await userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var doctorRepo = _unitOfWork.Repository<Doctor>();

        var existingUser = (await userRepo.FindAsync(u => u.Email == dto.Email)).FirstOrDefault();
        if (existingUser != null)
            throw new InvalidOperationException("Email already registered.");

        var doctor = new Doctor
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Specialty = dto.Specialty,
            Qualification = dto.Qualification,
            ExperienceYears = dto.ExperienceYears,
            ConsultationFee = dto.ConsultationFee,
            Bio = dto.Bio,
            Gender = dto.Gender,
            Location = dto.Location,
            LanguagesSpoken = dto.LanguagesSpoken,
            DepartmentId = dto.DepartmentId,
            MedicalLicenseNumber = dto.MedicalLicenseNumber,
            ProfileDocuments = dto.ProfileDocuments,
            ApprovalStatus = DoctorStatus.PendingReview,
            IsApproved = false,
            IsActive = true,
            IsAvailable = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        await doctorRepo.AddAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        var user = new AppUser
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Doctor,
            ReferenceId = doctor.Id,
            RefreshToken = _jwtService.GenerateRefreshToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
            IsActive = true
        };

        await userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(user);
        _logger.LogInformation("Doctor registered (pending approval): {Email}", dto.Email);

        string doctorRegEmailBody = EmailTemplates.BuildDoctorRegistrationReceivedEmail(
            $"{doctor.FirstName} {doctor.LastName}", doctor.Specialty, doctor.MedicalLicenseNumber);

        string adminRegEmailBody = EmailTemplates.BuildAdminDoctorRegistrationEmail(
            "Admin", $"{doctor.FirstName} {doctor.LastName}", doctor.Specialty, doctor.Email, doctor.MedicalLicenseNumber, DateTime.UtcNow);

        await _emailSms.SendEmailAsync(doctor.Email, "MediSphere Doctor Application Received", doctorRegEmailBody);
        await _emailSms.SendEmailAsync("admin@medisphere.com", "New Doctor Registration Pending Approval", adminRegEmailBody);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = user.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Role = user.Role.ToString(),
            Email = user.Email,
            UserId = user.Id,
            ReferenceId = doctor.Id
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();

        var user = (await userRepo.FindAsync(u => u.Email == dto.Email))
            .FirstOrDefault();

        if (user == null)
        {
            _logger.LogInformation("Password reset requested for unknown email.");
            return;
        }

        // Generate 6-digit OTP
        var otp = RandomNumberGenerator
            .GetInt32(100000, 999999)
            .ToString();

        var cacheKey = $"otp:reset-password:{user.Email.ToLowerInvariant()}";
        var otpData = new ResetPasswordOtpData
        {
            UserId = user.Id,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(otp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetTokenExpiryMinutes)
        };

        await _cacheService.SetAsync(cacheKey, otpData, TimeSpan.FromMinutes(PasswordResetTokenExpiryMinutes));

        var displayName = await ResolveUserDisplayNameAsync(user);

        var emailBody = EmailTemplates.BuildPasswordResetEmail(
            displayName,
            otp,
            PasswordResetTokenExpiryMinutes
        );

        await _emailSms.SendEmailAsync(
            user.Email,
            "MediSphere Password Reset OTP",
            emailBody);

        _logger.LogInformation(
            "Password reset OTP sent for user {UserId}",
            user.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var userRepo = _unitOfWork.Repository<AppUser>();
        var user = (await userRepo.FindAsync(u => u.Email == dto.Email)).FirstOrDefault()
            ?? throw new ArgumentException("Invalid or expired reset token.");

        var cacheKey = $"otp:reset-password:{dto.Email.ToLowerInvariant()}";
        var otpData = await _cacheService.GetAsync<ResetPasswordOtpData>(cacheKey);

        if (otpData == null || otpData.UserId != user.Id || otpData.ExpiresAt < DateTime.UtcNow)
        {
            throw new ArgumentException("Invalid or expired reset token.");
        }
        
        if (!BCrypt.Net.BCrypt.Verify(dto.Otp, otpData.OtpHash))
        {
            throw new ArgumentException("Invalid or expired reset token.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordHash = passwordHash;
        user.RefreshToken = string.Empty;
        user.RefreshTokenExpiry = DateTime.MinValue;
        await userRepo.UpdateAsync(user);

        await _cacheService.RemoveAsync(cacheKey);

        if (user.Role == UserRole.Patient && user.ReferenceId.HasValue)
        {
            var p = await _unitOfWork.Repository<Patient>().GetByIdAsync(user.ReferenceId.Value);
            if (p != null)
            {
                p.PasswordHash = passwordHash;
                await _unitOfWork.Repository<Patient>().UpdateAsync(p);
            }
        }
        else if (user.Role == UserRole.Doctor && user.ReferenceId.HasValue)
        {
            var d = await _unitOfWork.Repository<Doctor>().GetByIdAsync(user.ReferenceId.Value);
            if (d != null)
            {
                d.PasswordHash = passwordHash;
                await _unitOfWork.Repository<Doctor>().UpdateAsync(d);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }

    private async Task<string> ResolveUserDisplayNameAsync(AppUser user)
    {
        if (user.Role == UserRole.Patient && user.ReferenceId.HasValue)
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(user.ReferenceId.Value);
            if (patient != null)
                return $"{patient.FirstName} {patient.LastName}";
        }

        if (user.Role == UserRole.Doctor && user.ReferenceId.HasValue)
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(user.ReferenceId.Value);
            if (doctor != null)
                return $"Dr. {doctor.FirstName} {doctor.LastName}";
        }

        return user.Email;
    }
}

public class ResetPasswordOtpData
{
    public int UserId { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

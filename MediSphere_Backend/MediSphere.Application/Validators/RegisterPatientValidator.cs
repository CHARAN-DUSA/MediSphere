using FluentValidation;
using MediSphere.Application.DTOs.Auth;

namespace MediSphere.Application.Validators;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientDto>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])")
            .WithMessage("Password must contain uppercase, lowercase, number, and special character.");
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{9,14}$");
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateTime.Today);
        RuleFor(x => x.Gender).NotEmpty().Must(g => new[] { "Male", "Female", "Other" }.Contains(g));
    }
}

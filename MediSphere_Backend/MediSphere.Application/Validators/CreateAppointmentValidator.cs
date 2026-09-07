using FluentValidation;
using MediSphere.Application.DTOs.Appointment;

namespace MediSphere.Application.Validators;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentDto>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0);
        RuleFor(x => x.AppointmentDate).GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Appointment date must be today or in the future.");
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

using FluentValidation;
using MediSphere.Application.Interfaces;
using MediSphere.Application.Services;
using MediSphere.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace MediSphere.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISmartRecommendationService, SmartRecommendationService>();
        services.AddScoped<ISavedDoctorService, SavedDoctorService>();

        services.AddValidatorsFromAssemblyContaining<RegisterPatientValidator>();

        // Register AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}

using MediSphere.Application.Interfaces;
using MediSphere.Domain.Interfaces;
using MediSphere.Infrastructure.Configuration;
using MediSphere.Infrastructure.Persistence;
using MediSphere.Infrastructure.Services;
using MediSphere.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediSphere.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        config.GetConnectionString("DefaultConnection")));

        services.AddOptions<BrevoOptions>()
            .Bind(config.GetSection(BrevoOptions.SectionName))
            .PostConfigure(options =>
            {
                var smtp = new SmtpOptions
                {
                    Host = config["Smtp:Host"] ?? options.Smtp.Host,
                    Username = config["Smtp:Username"] ?? options.Smtp.Username,
                    Password = config["Smtp:Password"] ?? options.Smtp.Password
                };
                var portStr = config["Smtp:Port"] ?? config[$"{BrevoOptions.SectionName}:Smtp:Port"];
                if (int.TryParse(portStr, out var port))
                    smtp.Port = port;
                var sslStr = config["Smtp:EnableSsl"] ?? config[$"{BrevoOptions.SectionName}:Smtp:EnableSsl"];
                smtp.EnableSsl = !string.Equals(sslStr, "false", StringComparison.OrdinalIgnoreCase);
                options.Smtp = smtp;
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<BrevoOptions>, BrevoOptionsValidator>();
        services.AddSingleton<IAppUrlSettings, AppUrlSettings>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IJwtService, JwtService>();

        // Cache: Redis with in-memory fallback
        services.AddMemoryCache();
        services.AddScoped<ICacheService, RedisCacheService>();

        // File storage
        services.AddScoped<IFileStorageService>(serviceProvider =>
        {
            var webHostEnvironment =
                serviceProvider.GetRequiredService<
                    Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

            var baseUrl =
                config["AppBaseUrl"] ?? "https://localhost:5001";

            return new LocalFileStorageService(
                webHostEnvironment,
                baseUrl);
        });

        // Email queue and delivery
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddScoped<IEmailDeliveryService, BrevoNotificationService>();
        services.AddSingleton<IEmailSmsService, QueuedEmailSmsService>();
        services.AddHostedService<EmailQueueProcessorService>();
        services.AddHostedService<BackgroundTaskProcessorService>();

        // Razorpay Payments
        services.AddScoped<IPaymentService, RazorpayPaymentService>();

        // AES-256 Encryption Helper
        services.AddSingleton<EncryptionHelper>();

        // Background Jobs Worker (appointment reminders, queue resets)
        services.AddHostedService<BackgroundJobService>();

        // SignalR Service
        services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

        // SignalR
        services.AddSignalR();

        return services;
    }
}

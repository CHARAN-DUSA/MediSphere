using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediSphere.Infrastructure.Services;

public class BackgroundJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MediSphere Background Operations worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRemindersCheckAsync();
                await RunMidnightQueueResetCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during background tasks execution.");
            }

            // Run checks every 5 minutes to avoid excessive database polling
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task RunRemindersCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifier = scope.ServiceProvider.GetRequiredService<IEmailSmsService>();

        var reminderTimeMin = DateTime.UtcNow.AddHours(2).AddMinutes(-10);
        var reminderTimeMax = DateTime.UtcNow.AddHours(2).AddMinutes(10);

        var appointments = unitOfWork.Repository<Appointment>().Query()
            .Where(a => a.Status == AppointmentStatus.Pending)
            .ToList();

        foreach (var apt in appointments)
        {
            var aptDateTime = apt.AppointmentDate.Date.Add(apt.StartTime);
            
            // Check if appointment is exactly 2 hours from now
            if (aptDateTime >= reminderTimeMin && aptDateTime <= reminderTimeMax)
            {
                var patient = await unitOfWork.Repository<Patient>().GetByIdAsync(apt.PatientId);
                var doctor = await unitOfWork.Repository<Doctor>().GetByIdAsync(apt.DoctorId);

                if (patient != null && doctor != null)
                {
                    _logger.LogInformation($"[REPEATED JOB] Sending 2-hour appointment reminder to patient {patient.Email}");

                    string patientName = $"{patient.FirstName} {patient.LastName}";
                    string doctorName = $"{doctor.FirstName} {doctor.LastName}";
                    string dateStr = apt.AppointmentDate.ToString("yyyy-MM-dd");
                    string timeStr = apt.StartTime.ToString(@"hh\:mm");
                    var subject = "Upcoming Consultation Reminder - MediSphere";
                    var body = MediSphere.Application.Common.EmailTemplates.BuildAppointmentReminderEmail(
                        patientName, doctorName, doctor.Specialty, dateStr, timeStr, apt.QueueToken, apt.MeetingUrl);

                    await notifier.SendEmailAsync(patient.Email, subject, body);
                    await notifier.SendSmsAsync(patient.PhoneNumber, $"Reminder: Your consultation with Dr. {doctor.LastName} is scheduled for today at {apt.StartTime:hh\\:mm}. Token: #{apt.QueueToken}");
                }
            }
        }
    }

    private static DateTime _lastResetDate = DateTime.MinValue;

    private async Task RunMidnightQueueResetCheckAsync()
    {
        if (DateTime.Today == _lastResetDate) return;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _logger.LogInformation("[REPEATED JOB] Running midnight queue resetting...");

        var yesterday = DateTime.Today.AddDays(-1);
        var pendingApts = unitOfWork.Repository<Appointment>().Query()
            .Where(a => a.AppointmentDate.Date <= yesterday && (a.QueueStatus == "Waiting" || a.QueueStatus == "InConsultation"))
            .ToList();

        foreach (var apt in pendingApts)
        {
            apt.QueueStatus = "Completed";
            apt.Status = AppointmentStatus.Completed;
            await unitOfWork.Repository<Appointment>().UpdateAsync(apt);
        }

        await unitOfWork.SaveChangesAsync();
        _lastResetDate = DateTime.Today;
    }
}

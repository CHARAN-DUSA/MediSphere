using MediSphere.Application.Interfaces;
using MediSphere.Infrastructure.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediSphere.Infrastructure.Services;

public class EmailQueueProcessorService : BackgroundService
{
    private readonly EmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailQueueProcessorService> _logger;

    public EmailQueueProcessorService(
        EmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailQueueProcessorService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var emailTask = ProcessEmailsAsync(stoppingToken);
        var smsTask = ProcessSmsAsync(stoppingToken);
        await Task.WhenAll(emailTask, smsTask);
    }

    private async Task ProcessEmailsAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadEmailsAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var delivery = scope.ServiceProvider.GetRequiredService<IEmailDeliveryService>();
                await delivery.SendEmailAsync(message.ToEmail, message.Subject, message.BodyHtml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error processing queued email. Recipient={Recipient}, Subject={Subject}",
                    message.ToEmail, message.Subject);
            }
        }
    }

    private async Task ProcessSmsAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadSmsAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var delivery = scope.ServiceProvider.GetRequiredService<IEmailDeliveryService>();
                await delivery.SendSmsAsync(message.ToPhoneNumber, message.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error processing queued SMS. Recipient={Recipient}",
                    message.ToPhoneNumber);
            }
        }
    }
}

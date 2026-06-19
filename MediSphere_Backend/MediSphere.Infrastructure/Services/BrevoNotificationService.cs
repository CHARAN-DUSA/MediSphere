using System.Net.Http;
using System.Text;
using System.Text.Json;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using MediSphere.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediSphere.Infrastructure.Services;

public class BrevoNotificationService : IEmailDeliveryService
{
    private static readonly HttpClient HttpClient = new();
    private readonly BrevoOptions _options;
    private readonly SmtpOptions _smtpFromConfig;
    private readonly bool _isApiEnabled;
    private readonly bool _isSmtpConfigured;
    private readonly bool _isDevelopment;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BrevoNotificationService> _logger;

    public BrevoNotificationService(
        IOptions<BrevoOptions> options,
        IConfiguration config,
        IHostEnvironment environment,
        ICacheService cacheService,
        ILogger<BrevoNotificationService> logger)
    {
        _options = options.Value;
        _cacheService = cacheService;
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();
        _isApiEnabled = BrevoOptionsValidator.IsValidApiKey(_options.ApiKey);

        _smtpFromConfig = ResolveSmtpOptions(config);
        _isSmtpConfigured = BrevoOptionsValidator.IsSmtpConfigured(new BrevoOptions
        {
            Smtp = _smtpFromConfig
        });
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        string? errorMessage = null;
        var sent = false;
        var channel = "none";

        if (_isApiEnabled)
        {
            try
            {
                sent = await TrySendViaBrevoApiAsync(toEmail, subject, bodyHtml);
                if (sent) channel = "brevo-api";
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "Brevo API email delivery failed. Recipient={Recipient}, Subject={Subject}", toEmail, subject);
            }
        }

        if (!sent && _isSmtpConfigured)
        {
            try
            {
                sent = await TrySendViaSmtpAsync(toEmail, subject, bodyHtml);
                if (sent) channel = "smtp";
            }
            catch (Exception ex)
            {
                errorMessage = string.IsNullOrEmpty(errorMessage) ? ex.Message : $"{errorMessage}; {ex.Message}";
                _logger.LogError(ex, "SMTP email delivery failed. Recipient={Recipient}, Subject={Subject}", toEmail, subject);
            }
        }

        if (sent)
        {
            await PersistEmailLogAsync(toEmail, subject, EmailDeliveryStatus.Sent, null, DateTime.UtcNow);
            _logger.LogInformation(
                "Email delivered. Recipient={Recipient}, Subject={Subject}, Channel={Channel}",
                toEmail, subject, channel);
            return;
        }

        if (_isDevelopment)
        {
            await PersistEmailLogAsync(toEmail, subject, EmailDeliveryStatus.Sandbox, errorMessage, null);
            _logger.LogInformation(
                "Sandbox email recorded (Development only). Recipient={Recipient}, Subject={Subject}",
                toEmail, subject);
            return;
        }

        await PersistEmailLogAsync(toEmail, subject, EmailDeliveryStatus.Failed, errorMessage ?? "No delivery channel available", null);
        _logger.LogError(
            "Email delivery failed in production. Recipient={Recipient}, Subject={Subject}, Error={Error}",
            toEmail, subject, errorMessage ?? "No delivery channel configured");
        throw new InvalidOperationException($"Email delivery failed for {toEmail}.");
    }

    public async Task SendSmsAsync(string toPhoneNumber, string message)
    {
        if (!_isApiEnabled)
        {
            if (_isDevelopment)
            {
                _logger.LogInformation(
                    "Sandbox SMS recorded (Development only). Recipient={Recipient}",
                    toPhoneNumber);
                return;
            }

            _logger.LogError("SMS delivery failed in production. Recipient={Recipient}, Error=No Brevo API key configured", toPhoneNumber);
            throw new InvalidOperationException($"SMS delivery failed for {toPhoneNumber}.");
        }

        try
        {
            var requestUrl = "https://api.brevo.com/v3/transactionalSMS/sms";
            var payload = new
            {
                type = "transactional",
                sender = "MediSphere",
                recipient = toPhoneNumber,
                content = message
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("api-key", _options.ApiKey);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "SMS delivery failed. Recipient={Recipient}, StatusCode={StatusCode}, Details={Details}",
                    toPhoneNumber, response.StatusCode, errorText);
                throw new InvalidOperationException($"SMS delivery failed with status {response.StatusCode}.");
            }

            _logger.LogInformation("SMS delivered. Recipient={Recipient}", toPhoneNumber);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "SMS delivery failed. Recipient={Recipient}", toPhoneNumber);
            throw;
        }
    }

    private async Task<bool> TrySendViaBrevoApiAsync(string toEmail, string subject, string bodyHtml)
    {
        var requestUrl = "https://api.brevo.com/v3/smtp/email";
        var payload = new
        {
            sender = new { name = _options.SenderName, email = _options.SenderEmail },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = bodyHtml
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("api-key", _options.ApiKey);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await HttpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return true;

        var errorText = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "Brevo API rejected email. Recipient={Recipient}, StatusCode={StatusCode}, Details={Details}",
            toEmail, response.StatusCode, errorText);
        return false;
    }

    private async Task<bool> TrySendViaSmtpAsync(string toEmail, string subject, string bodyHtml)
    {
        using var mailMessage = new System.Net.Mail.MailMessage();
        mailMessage.From = new System.Net.Mail.MailAddress(_options.SenderEmail, _options.SenderName);
        mailMessage.To.Add(toEmail);
        mailMessage.Subject = subject;
        mailMessage.Body = bodyHtml;
        mailMessage.IsBodyHtml = true;

        using var smtpClient = new System.Net.Mail.SmtpClient(_smtpFromConfig.Host, _smtpFromConfig.Port);
        if (!string.IsNullOrEmpty(_smtpFromConfig.Username))
        {
            smtpClient.Credentials = new System.Net.NetworkCredential(
                _smtpFromConfig.Username, _smtpFromConfig.Password);
        }
        smtpClient.EnableSsl = _smtpFromConfig.EnableSsl;

        await smtpClient.SendMailAsync(mailMessage);
        return true;
    }

    private async Task PersistEmailLogAsync(
        string recipient,
        string subject,
        EmailDeliveryStatus status,
        string? errorMessage,
        DateTime? sentAt)
    {
        var log = new EmailLog
        {
            Recipient = recipient,
            Subject = subject,
            Status = status,
            ErrorMessage = errorMessage,
            SentAt = sentAt,
            CreatedAt = DateTime.UtcNow
        };

        var logId = Guid.NewGuid().ToString("N");
        var cacheKey = $"emaillog:{recipient}:{logId}";
        await _cacheService.SetAsync(cacheKey, log, TimeSpan.FromDays(7));
    }

    private static SmtpOptions ResolveSmtpOptions(IConfiguration config)
    {
        var smtp = new SmtpOptions
        {
            Host = config["Smtp:Host"] ?? config["Brevo:Smtp:Host"],
            Username = config["Smtp:Username"] ?? config["Brevo:Smtp:Username"],
            Password = config["Smtp:Password"] ?? config["Brevo:Smtp:Password"]
        };

        var portStr = config["Smtp:Port"] ?? config["Brevo:Smtp:Port"];
        if (int.TryParse(portStr, out var port))
            smtp.Port = port;

        var sslStr = config["Smtp:EnableSsl"] ?? config["Brevo:Smtp:EnableSsl"];
        smtp.EnableSsl = !string.Equals(sslStr, "false", StringComparison.OrdinalIgnoreCase);

        return smtp;
    }
}

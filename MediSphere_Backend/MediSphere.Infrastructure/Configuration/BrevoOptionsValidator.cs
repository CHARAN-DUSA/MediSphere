using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MediSphere.Infrastructure.Configuration;

public class BrevoOptionsValidator : IValidateOptions<BrevoOptions>
{
    private readonly IHostEnvironment _environment;

    public BrevoOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, BrevoOptions options)
    {
        if (_environment.IsDevelopment())
            return ValidateOptionsResult.Success;

        var hasApiKey = IsValidApiKey(options.ApiKey);
        var hasSmtp = IsSmtpConfigured(options);

        if (!hasApiKey && !hasSmtp)
        {
            return ValidateOptionsResult.Fail(
                "Production requires Brevo:ApiKey or fully configured SMTP (Brevo:Smtp or Smtp section). " +
                "Sandbox mode is only available in Development.");
        }

        if (string.IsNullOrWhiteSpace(options.SenderEmail))
            return ValidateOptionsResult.Fail("Brevo:SenderEmail is required.");

        return ValidateOptionsResult.Success;
    }

    internal static bool IsValidApiKey(string? apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey) && apiKey != "YOUR-BREVO-API-KEY-HERE";

    internal static bool IsSmtpConfigured(BrevoOptions options) =>
        !string.IsNullOrWhiteSpace(options.Smtp.Host) &&
        options.Smtp.Port > 0 &&
        options.Smtp.Port <= 65535;
}

using MediSphere.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MediSphere.Infrastructure.Configuration;

public class AppUrlSettings : IAppUrlSettings
{
    public AppUrlSettings(IConfiguration configuration)
    {
        FrontendBaseUrl = configuration["FrontendBaseUrl"]
            ?? configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault()
            ?? "http://localhost:4200";
    }

    public string FrontendBaseUrl { get; }
}

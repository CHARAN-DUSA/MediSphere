using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace MediSphere.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT Bearer authentication using the Jwt:Key, Jwt:Issuer, and Jwt:Audience
    /// configuration values from appsettings.json.
    /// </summary>
    public static IServiceCollection AddMediSphereJwtAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = config["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Registers the CORS policy named "MediSpherePolicy" allowing origins from
    /// AllowedOrigins in configuration (defaults to http://localhost:4200).
    /// </summary>
    public static IServiceCollection AddMediSphereCors(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("MediSpherePolicy", policy =>
            {
                policy
                    .WithOrigins(
                        config.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:4200" })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers fixed-window rate limiters:
    /// - "strict"  → 5 requests / 10 seconds  (for auth endpoints)
    /// - "general" → 100 requests / 1 minute   (for general API calls)
    /// </summary>
    public static IServiceCollection AddMediSphereRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("strict", opt =>
            {
                opt.Window      = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 5;
                opt.QueueLimit  = 0;
            });

            options.AddFixedWindowLimiter("general", opt =>
            {
                opt.Window               = TimeSpan.FromMinutes(1);
                opt.PermitLimit          = 100;
                opt.QueueLimit           = 10;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        return services;
    }

    /// <summary>
    /// Registers Swagger/OpenAPI documentation with a Bearer token security definition
    /// so JWT-protected endpoints can be tested directly from the Swagger UI.
    /// </summary>
    public static IServiceCollection AddMediSphereSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "MediSphere API",
                Version     = "v1",
                Description = "Hospital Booking System API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name        = "Authorization",
                Type        = SecuritySchemeType.Http,
                Scheme      = "bearer",
                BearerFormat = "JWT",
                In          = ParameterLocation.Header,
                Description = "Enter JWT token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}

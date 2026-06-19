using FluentValidation.AspNetCore;
using MediSphere.API.Extensions;
using MediSphere.API.Middleware;
using MediSphere.Application;
using MediSphere.Infrastructure;
using MediSphere.Infrastructure.Hubs;
using MediSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//
// CONFIGURATION
//
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

//
// SERILOG
//
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/medisphere-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

//
// SERVICES
//
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddMediSphereJwtAuthentication(builder.Configuration);
builder.Services.AddMediSphereCors(builder.Configuration);
builder.Services.AddMediSphereRateLimiting();
builder.Services.AddMediSphereSwagger();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

//
// BUILD APP
//
var app = builder.Build();

//
// DATABASE MIGRATION
//
try
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Migration skipped: {ex.Message}");
}
//
// MIDDLEWARE PIPELINE
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MediSphere API v1"));
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("MediSpherePolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapHub<VideoConsultationHub>("/hubs/video");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
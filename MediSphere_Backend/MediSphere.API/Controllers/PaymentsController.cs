using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediSphere.Application.Interfaces;
using MediSphere.Application.DTOs.Common;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly IEmailSmsService _emailSms;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        IEmailSmsService emailSms,
        IBackgroundTaskQueue backgroundTaskQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _config = config;
        _emailSms = emailSms;
        _backgroundTaskQueue = backgroundTaskQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PaymentConfigResponse>> GetPaymentConfig()
    {
        var keyId = _config["Razorpay:KeyId"];
        var isSandbox = string.IsNullOrWhiteSpace(keyId)
            || keyId == "rzp_test_dummy_key_id"
            || keyId == "YOUR-RAZORPAY-KEY-ID";

        return Ok(ApiResponse<PaymentConfigResponse>.Ok(new PaymentConfigResponse(
            KeyId: isSandbox ? string.Empty : keyId!,
            IsSandbox: isSandbox
        )));
    }

    [HttpPost("create-order")]
    public async Task<ActionResult<ApiResponse<string>>> CreateOrder([FromBody] CreateOrderRequest req)
    {
        if (req.Amount <= 0)
        {
            return BadRequest(ApiResponse<string>.Fail("Amount must be greater than zero."));
        }

        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(req.AppointmentId);
            if (appointment == null)
            {
                return NotFound(ApiResponse<string>.Fail("Appointment not found."));
            }

            var orderId = await _paymentService.CreateOrderAsync(req.AppointmentId, req.Amount);
            
            // Save order ID to appointment
            appointment.RazorpayOrderId = orderId;
            await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<string>.Ok(orderId, "Order created successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment order.");
            return StatusCode(500, ApiResponse<string>.Fail($"Payment order creation failed: {ex.Message}"));
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        _logger.LogInformation("Razorpay Webhook triggered.");

        string signature = Request.Headers["X-Razorpay-Signature"].ToString();
        string secret = _config["Razorpay:WebhookSecret"] ?? "dummy_webhook_secret";

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        string payload = await reader.ReadToEndAsync();

        bool isValid = _paymentService.VerifyWebhookSignature(payload, signature, secret);
        if (!isValid)
        {
            _logger.LogWarning("Razorpay Webhook signature verification failed.");
            return BadRequest("Invalid Signature");
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventType = root.GetProperty("event").GetString();

            if (eventType == "payment.captured")
            {
                var paymentObj = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
                string orderId = paymentObj.GetProperty("order_id").GetString() ?? string.Empty;
                string paymentId = paymentObj.GetProperty("id").GetString() ?? string.Empty;
                decimal amountInPaise = paymentObj.GetProperty("amount").GetDecimal();
                decimal amount = amountInPaise / 100.0m;

                await ProcessSuccessfulPaymentAsync(orderId, paymentId, amount);
            }

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Razorpay Webhook callback.");
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("simulate-webhook")]
    public async Task<ActionResult<ApiResponse<object>>> SimulateWebhook([FromBody] SimulateWebhookRequest req)
    {
        _logger.LogInformation("Simulating Razorpay Webhook for Order: {OrderId}", req.OrderId);
        
        try
        {
            bool processed = await ProcessSuccessfulPaymentAsync(req.OrderId, req.PaymentId, req.Amount);
            if (!processed)
            {
                return NotFound(ApiResponse<object>.Fail("No pending appointment found matching the specified Razorpay order ID."));
            }

            return Ok(ApiResponse<object>.Ok(null!, "Simulated payment captured and processed successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate webhook.");
            return StatusCode(500, ApiResponse<object>.Fail($"Simulation failed: {ex.Message}"));
        }
    }

    [HttpPost("payment-failed")]
    public async Task<ActionResult<ApiResponse<object>>> PaymentFailed([FromBody] PaymentFailedRequest req)
    {
        _logger.LogWarning("Payment failed for Razorpay OrderId: {OrderId}", req.OrderId);
        
        try
        {
            var appointment = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.RazorpayOrderId == req.OrderId);

            if (appointment == null)
            {
                return NotFound(ApiResponse<object>.Fail("No appointment found matching the specified Razorpay order ID."));
            }

            appointment.PaymentStatus = "Failed";
            await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

            var transaction = new PaymentTransaction
            {
                AppointmentId = appointment.Id,
                RazorpayOrderId = req.OrderId,
                RazorpayPaymentId = req.PaymentId ?? string.Empty,
                Amount = appointment.Fee,
                Status = "Failed",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            if (appointment.Patient != null)
            {
                string patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
                string doctorName = appointment.Doctor != null ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}" : "Doctor";
                string emailBody = MediSphere.Application.Common.EmailTemplates.BuildPaymentFailedEmail(
                    patientName, doctorName, appointment.Fee, req.OrderId);

                await _emailSms.SendEmailAsync(appointment.Patient.Email, "MediSphere Payment Failed", emailBody);
            }

            return Ok(ApiResponse<object>.Ok(null!, "Payment failure recorded successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record payment failure.");
            return StatusCode(500, ApiResponse<object>.Fail($"Recording failure failed: {ex.Message}"));
        }
    }

    private async Task<bool> ProcessSuccessfulPaymentAsync(string orderId, string paymentId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(orderId)) return false;

        var appointment = await _unitOfWork.Repository<Appointment>().Query()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.RazorpayOrderId == orderId);

        if (appointment == null)
        {
            _logger.LogWarning("No appointment found matching Razorpay OrderId: {OrderId}", orderId);
            return false;
        }

        if (appointment.PaymentStatus == "Paid")
        {
            _logger.LogInformation("Appointment {Id} is already marked as Paid.", appointment.Id);
            return true;
        }

        // Retrieve dynamic settings: CommissionRate, PlatformFeeRate, TaxRate
        var commissionRateSetting = await _unitOfWork.Repository<SystemSetting>().Query().FirstOrDefaultAsync(s => s.Key == "CommissionRate");
        var platformFeeSetting = await _unitOfWork.Repository<SystemSetting>().Query().FirstOrDefaultAsync(s => s.Key == "PlatformFeeRate" || s.Key == "PlatformFee");
        var taxRateSetting = await _unitOfWork.Repository<SystemSetting>().Query().FirstOrDefaultAsync(s => s.Key == "TaxRate");

        decimal commissionRate = 15.0m;
        if (commissionRateSetting != null && decimal.TryParse(commissionRateSetting.Value, out var cr))
        {
            commissionRate = cr;
        }

        decimal platformFeeRate = 2.0m; // default 2%
        if (platformFeeSetting != null && decimal.TryParse(platformFeeSetting.Value, out var pf))
        {
            platformFeeRate = pf;
        }

        decimal taxRate = 18.0m; // default 18%
        if (taxRateSetting != null && decimal.TryParse(taxRateSetting.Value, out var tr))
        {
            taxRate = tr;
        }

        decimal grossAmount = amount;
        decimal platformFee = Math.Round(grossAmount * (platformFeeRate / 100.0m), 2);
        decimal taxAmount = Math.Round(grossAmount * (taxRate / 100.0m), 2);
        decimal adminCommission = Math.Round(grossAmount * (commissionRate / 100.0m), 2);
        decimal doctorEarnings = grossAmount - adminCommission;
        decimal netDoctorAmount = grossAmount - adminCommission - platformFee - taxAmount;

        // Process referral rewards and loyalty points
        if (appointment.Patient != null)
        {
            var patient = appointment.Patient;

            // Earn booking loyalty points (10 points per booking)
            patient.RewardPoints += 10;
            var bookingRewardLog = new PatientRewardLog
            {
                PatientId = patient.Id,
                Points = 10,
                Action = "Loyalty Booking",
                Description = "Earned 10 points for booking appointment.",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<PatientRewardLog>().AddAsync(bookingRewardLog);

            // Check if this is their first paid appointment
            var paidAppointmentsCount = await _unitOfWork.Repository<Appointment>().Query()
                .CountAsync(a => a.PatientId == patient.Id && a.PaymentStatus == "Paid");

            if (paidAppointmentsCount == 0 && !string.IsNullOrWhiteSpace(patient.ReferredByCode))
            {
                var referrer = await _unitOfWork.Repository<Patient>().Query()
                    .FirstOrDefaultAsync(p => p.ReferralCode == patient.ReferredByCode);

                if (referrer != null)
                {
                    // Credit referrer 100 points
                    referrer.RewardPoints += 100;
                    var referrerRewardLog = new PatientRewardLog
                    {
                        PatientId = referrer.Id,
                        Points = 100,
                        Action = "Referral Bonus",
                        Description = $"Earned 100 points for referring {patient.FirstName} {patient.LastName}.",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<PatientRewardLog>().AddAsync(referrerRewardLog);
                    await _unitOfWork.Repository<Patient>().UpdateAsync(referrer);

                    // Credit referee patient 50 welcome points
                    patient.RewardPoints += 50;
                    var refereeRewardLog = new PatientRewardLog
                    {
                        PatientId = patient.Id,
                        Points = 50,
                        Action = "Welcome Bonus",
                        Description = "Earned 50 points welcome bonus for using referral code.",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<PatientRewardLog>().AddAsync(refereeRewardLog);
                }
            }

            await _unitOfWork.Repository<Patient>().UpdateAsync(patient);
        }

        // Update Database States
        appointment.PaymentStatus = "Paid";
        appointment.Status = AppointmentStatus.Confirmed; // Confirm booking on successful payment
        await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);

        var transaction = new PaymentTransaction
        {
            AppointmentId = appointment.Id,
            RazorpayOrderId = orderId,
            RazorpayPaymentId = paymentId,
            Amount = amount,
            GrossAmount = grossAmount,
            AdminCommission = adminCommission,
            DoctorEarnings = doctorEarnings,
            PlatformFee = platformFee,
            TaxAmount = taxAmount,
            NetDoctorAmount = netDoctorAmount,
            Status = "Success",
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Payment successful. Confirmed Appointment ID: {AptId}, TransId: {TransId}", appointment.Id, transaction.Id);

        // Fire Email/SMS and SignalR updates
        if (appointment.Patient != null)
        {
            string patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
            string doctorName = appointment.Doctor != null ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}" : "Doctor";
            string dateStr = appointment.AppointmentDate.ToString("yyyy-MM-dd");
            string timeStr = appointment.StartTime.ToString(@"hh\:mm");

            string patientEmailBody = MediSphere.Application.Common.EmailTemplates.BuildPaymentCapturedEmail(
                patientName, doctorName, amount, adminCommission, doctorEarnings, taxAmount, paymentId, dateStr, timeStr);

            string doctorEmailBody = appointment.Doctor != null ? MediSphere.Application.Common.EmailTemplates.BuildDoctorNewAppointmentEmail(
                doctorName, patientName, dateStr, timeStr, appointment.QueueToken, appointment.Reason, amount) : string.Empty;

            await _emailSms.SendEmailAsync(appointment.Patient.Email, "MediSphere Payment Confirmation", patientEmailBody);
            if (appointment.Doctor != null && !string.IsNullOrEmpty(appointment.Doctor.Email))
            {
                await _emailSms.SendEmailAsync(appointment.Doctor.Email, "New Appointment Booked", doctorEmailBody);
            }
            await _emailSms.SendSmsAsync(
                appointment.Patient.PhoneNumber,
                $"MediSphere: Payment of INR {amount} received! Appointment with Dr. {doctorName} is confirmed.");

            var doctorId = appointment.DoctorId;
            var departmentId = appointment.Doctor?.DepartmentId ?? 0;
            var patientId = appointment.PatientId;
            var queueToken = appointment.QueueToken;

            await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async ct =>
            {
                using var scope = _scopeFactory.CreateScope();
                var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notifications.SendToRoleReferenceAsync(
                    UserRole.Doctor,
                    doctorId,
                    "Appointment Booked",
                    $"New appointment booked by {patientName} on {dateStr}",
                    "Booking");

                await notifications.SendToRoleReferenceAsync(
                    UserRole.Patient,
                    patientId,
                    "Appointment Booked",
                    $"Your appointment with Dr. {doctorName} on {dateStr} is confirmed.",
                    "Booking");

                await notifications.SendToRoleReferenceAsync(
                    UserRole.Doctor,
                    doctorId,
                    "Payment Received",
                    $"Payment of INR {amount} received from patient {patientName} for consultation on {dateStr}",
                    "Payment");

                await notifications.SendToRoleReferenceAsync(
                    UserRole.Patient,
                    patientId,
                    "Payment Sent",
                    $"Payment of INR {amount} sent for your consultation with Dr. {doctorName}.",
                    "Payment");

                await signalR.NotifyQueueUpdateAsync(doctorId, departmentId);
                await signalR.NotifyQueuePositionChangedAsync(patientId, queueToken, "Waiting");
            });
        }

        return true;
    }
}

public record PaymentConfigResponse(string KeyId, bool IsSandbox);
public record CreateOrderRequest(int AppointmentId, decimal Amount);
public record SimulateWebhookRequest(string OrderId, string PaymentId, decimal Amount);
public record PaymentFailedRequest(string OrderId, string? PaymentId);

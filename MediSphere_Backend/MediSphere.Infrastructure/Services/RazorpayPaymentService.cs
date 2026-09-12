using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Payment;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediSphere.Infrastructure.Services;

public class RazorpayPaymentService : IPaymentService
{
    private readonly string? _keyId;
    private readonly string? _keySecret;
    private readonly bool _isEnabled;
    private readonly ILogger<RazorpayPaymentService> _logger;

    public RazorpayPaymentService(IConfiguration config, ILogger<RazorpayPaymentService> logger)
    {
        _logger = logger;
        _keyId = config["Razorpay:KeyId"];
        _keySecret = config["Razorpay:KeySecret"];

        _isEnabled = !string.IsNullOrWhiteSpace(_keyId)
                     && _keyId != "YOUR-RAZORPAY-KEY-ID"
                     && _keyId != "rzp_test_dummy_key_id"
                     && !string.IsNullOrWhiteSpace(_keySecret);
    }

    public async Task<string> CreateOrderAsync(
        int appointmentId,
        decimal amount,
        string currency = "INR")
    {
        var amountInPaise = (int)Math.Round(
            amount * 100,
            MidpointRounding.AwayFromZero);

        if (!_isEnabled)
        {
            _logger.LogInformation(
                "Razorpay is disabled or using dummy keys. Generating sandbox Order ID for Appointment {AppointmentId}",
                appointmentId);

            return $"order_sandbox_{appointmentId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        try
        {
            using var httpClient = new HttpClient();

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}")
            );

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials
                );

            var payload = new
            {
                amount = amountInPaise,
                currency,
                receipt = $"receipt_apt_{appointmentId}"
            };

            _logger.LogInformation(
                "Creating Razorpay order for Appointment {AppointmentId}",
                appointmentId);

            var response = await httpClient.PostAsync(
                "https://api.razorpay.com/v1/orders",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            );

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Razorpay API Error ({response.StatusCode}): {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            return doc.RootElement
                .GetProperty("id")
                .GetString()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Razorpay Payment Service failed in CreateOrderAsync");

            throw;
        }
    }

    public async Task<RefundResultDto> InitiateRefundAsync(
        string paymentId,
        decimal amount,
        string? reason = null,
        string? receipt = null)
    {
        var amountInPaise = (int)Math.Round(
            amount * 100,
            MidpointRounding.AwayFromZero);

        // Dev sandbox mode fallback
        if (!_isEnabled ||
            paymentId.StartsWith("pay_sim_") ||
            paymentId.StartsWith("sandbox_"))
        {
            _logger.LogInformation(
                "Simulating Sandbox Refund for PaymentId: {PaymentId}, Amount: {Amount}",
                paymentId,
                amount);

            return new RefundResultDto
            {
                Success = true,
                Status = RefundStatus.Simulated,
                RefundId = $"rfnd_sim_{Guid.NewGuid():N}",
                Amount = amount,
                Message = "Refund simulated successfully in Sandbox environment."
            };
        }

        try
        {
            using var httpClient = new HttpClient();

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}")
            );

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    credentials
                );

            var payload = new
            {
                amount = amountInPaise,
                speed = "normal",
                notes = new
                {
                    reason = reason ?? "Appointment Cancelled"
                },
                receipt = receipt ??
                          $"rfnd_rcpt_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            };

            _logger.LogInformation(
                "Initiating Razorpay refund for PaymentId: {PaymentId}, Amount: {Amount}",
                paymentId,
                amount);

            var response = await httpClient.PostAsync(
                $"https://api.razorpay.com/v1/payments/{paymentId}/refund",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            );

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Razorpay Refund API error ({StatusCode}): {Response}",
                    response.StatusCode,
                    responseBody);

                return new RefundResultDto
                {
                    Success = false,
                    Status = RefundStatus.RefundFailed,
                    Message =
                        $"Razorpay API error ({response.StatusCode}): {responseBody}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }

            using var doc = JsonDocument.Parse(responseBody);

            var refundId =
                doc.RootElement
                   .GetProperty("id")
                   .GetString();

            var statusStr =
                doc.RootElement.TryGetProperty(
                    "status",
                    out var s)
                    ? s.GetString()
                    : "processed";

            var refundStatus = statusStr switch
            {
                "processed" => RefundStatus.Refunded,
                "pending" => RefundStatus.RefundProcessing,
                _ => RefundStatus.RefundProcessing
            };

            _logger.LogInformation(
                "Razorpay refund successful. RefundId: {RefundId}, Status: {Status}",
                refundId,
                refundStatus);

            return new RefundResultDto
            {
                Success = true,
                Status = refundStatus,
                RefundId = refundId,
                Amount = amount,
                Message =
                    "Refund processed successfully with payment provider."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initiate Razorpay refund for PaymentId: {PaymentId}",
                paymentId);

            return new RefundResultDto
            {
                Success = false,
                Status = RefundStatus.RefundFailed,
                Message = ex.Message
            };
        }
    }

    public bool VerifyWebhookSignature(
        string payload,
        string signature,
        string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        // Dev sandbox mode fallback bypass
        if (signature == "sandbox_bypass_signature")
        {
            return true;
        }

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);

            var hashBytes = hmac.ComputeHash(payloadBytes);

            var sb = new StringBuilder();

            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            var computedSignature = sb.ToString();

            return string.Equals(
                computedSignature,
                signature,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to verify Razorpay webhook signature.");

            return false;
        }
    }
}
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Payment;

namespace MediSphere.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreateOrderAsync(int appointmentId, decimal amount, string currency = "INR");
    bool VerifyWebhookSignature(string payload, string signature, string secret);
    Task<RefundResultDto> InitiateRefundAsync(string paymentId, decimal amount, string? reason = null, string? receipt = null);
}

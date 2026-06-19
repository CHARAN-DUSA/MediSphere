using System.Threading.Tasks;

namespace MediSphere.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreateOrderAsync(int appointmentId, decimal amount, string currency = "INR");
    bool VerifyWebhookSignature(string payload, string signature, string secret);
}

using MediSphere.Domain.Common;

namespace MediSphere.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal AdminCommission { get; set; }
    public decimal DoctorEarnings { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetDoctorAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Refunded
}

using System;

namespace MediSphere.Application.DTOs.Appointment;

public class PaymentTransactionDto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

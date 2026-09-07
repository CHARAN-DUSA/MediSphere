using MediSphere.Domain.Enums;

namespace MediSphere.Application.DTOs.Payment;

public class RefundResultDto
{
    public bool Success { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.NotApplicable;
    public string? RefundId { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

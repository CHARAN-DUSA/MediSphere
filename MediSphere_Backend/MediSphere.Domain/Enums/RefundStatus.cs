namespace MediSphere.Domain.Enums;

public enum RefundStatus
{
    NotApplicable = 0,
    RefundRequested = 1,    // Cancellation done, refund awaiting initiation
    RefundProcessing = 2,   // Submitted to Razorpay, awaiting provider confirmation
    Refunded = 3,           // Provider confirmed (refund.processed webhook received)
    RefundFailed = 4,       // Provider reported failure
    Simulated = 5,          // Sandbox / simulated refund (sandbox environments)
    PartiallyRefunded = 6   // Partial refund confirmed by provider
}

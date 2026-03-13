namespace Fcg.Notification.Contracts.Payloads;

public sealed class PaymentFailedEmailPayload : PaymentEmailPayloadBase
{
    public string? FailureReason { get; set; }
}

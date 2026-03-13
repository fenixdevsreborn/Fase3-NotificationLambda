namespace Fcg.Notification.Contracts;

/// <summary>Nomes de tipos de payload para deserialização. Alinhado com PaymentNotificationEvent da Payments API.</summary>
public static class PayloadTypeNames
{
    public const string PaymentApproved = "PaymentApprovedEmailPayload";
    public const string PaymentFailed = "PaymentFailedEmailPayload";
}

using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Contracts.Messages;
using Fcg.Notification.Contracts.Payloads;

namespace Fcg.Notification.Infrastructure.Payloads;

public sealed class PayloadValidator : IPayloadValidator
{
    public IReadOnlyList<string>? Validate(NotificationMessage message, object payload)
    {
        var errors = new List<string>();
        if (payload is PaymentApprovedEmailPayload approved)
            ValidatePaymentPayload(approved, errors);
        else if (payload is PaymentFailedEmailPayload failed)
            ValidatePaymentPayload(failed, errors);
        return errors.Count > 0 ? errors : null;
    }

    private static void ValidatePaymentPayload(PaymentEmailPayloadBase p, List<string> errors)
    {
        if (p.PaymentId == Guid.Empty)
            errors.Add("PaymentId is required.");
        if (string.IsNullOrWhiteSpace(p.UserEmail))
            errors.Add("UserEmail is required for delivery.");
        if (p.Amount < 0)
            errors.Add("Amount must be non-negative.");
    }
}

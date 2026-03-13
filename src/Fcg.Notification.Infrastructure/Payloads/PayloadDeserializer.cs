using System.Text.Json;
using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Contracts.Messages;
using Fcg.Notification.Contracts.Payloads;
using Fcg.Notification.Contracts;

namespace Fcg.Notification.Infrastructure.Payloads;

public sealed class PayloadDeserializer : IPayloadDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public object? Deserialize(NotificationMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Payload)) return null;
        var typeName = message.PayloadType?.Trim() ?? string.Empty;
        return typeName switch
        {
            PayloadTypeNames.PaymentApproved => JsonSerializer.Deserialize<PaymentApprovedEmailPayload>(message.Payload, JsonOptions),
            PayloadTypeNames.PaymentFailed => JsonSerializer.Deserialize<PaymentFailedEmailPayload>(message.Payload, JsonOptions),
            _ => null
        };
    }
}

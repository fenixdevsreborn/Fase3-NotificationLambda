using Fcg.Notification.Contracts.Messages;

namespace Fcg.Notification.Application.Abstractions;

/// <summary>Deserializa o campo Payload (JSON) para o tipo indicado por PayloadType. Retorna null se tipo desconhecido.</summary>
public interface IPayloadDeserializer
{
    object? Deserialize(NotificationMessage message);
}

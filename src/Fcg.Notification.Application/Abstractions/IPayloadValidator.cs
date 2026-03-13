using Fcg.Notification.Contracts.Messages;

namespace Fcg.Notification.Application.Abstractions;

/// <summary>Valida o payload de uma mensagem conforme PayloadType. Retorna erros de validação ou null se válido.</summary>
public interface IPayloadValidator
{
    IReadOnlyList<string>? Validate(NotificationMessage message, object payload);
}

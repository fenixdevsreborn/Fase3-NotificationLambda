using Fcg.Notification.Contracts.Enums;

namespace Fcg.Notification.Contracts.Messages;

/// <summary>Modelo padronizado de mensagem para a fila. Payload é JSON; PayloadType indica o tipo para deserialização.</summary>
public sealed class NotificationMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string Recipient { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string PayloadType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

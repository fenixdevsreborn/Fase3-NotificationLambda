using Fcg.Notification.Contracts.Messages;

namespace Fcg.Notification.Application.Abstractions;

/// <summary>Processa uma mensagem de notificação: resolve template, valida payload, renderiza e envia.</summary>
public interface INotificationHandler
{
    Task<NotificationResult> HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public enum NotificationResultStatus
{
    Sent,
    TemplateNotFound,
    ValidationFailed,
    SendFailed,
    SkippedDuplicate
}

public sealed class NotificationResult
{
    public NotificationResultStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<string>? ValidationErrors { get; set; }
}

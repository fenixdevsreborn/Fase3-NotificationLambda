/// <summary>Store para idempotência: evita reenvio para o mesmo MessageId. Implementação pode ser em memória (Lambda) ou DynamoDB.</summary>
public interface ISentNotificationStore
{
    Task<bool> TryMarkSentAsync(string messageId, CancellationToken cancellationToken = default);
    Task<bool> WasSentAsync(string messageId, CancellationToken cancellationToken = default);
}

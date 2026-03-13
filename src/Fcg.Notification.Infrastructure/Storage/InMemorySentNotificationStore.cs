using System.Collections.Concurrent;
using Fcg.Notification.Application.Abstractions;

namespace Fcg.Notification.Infrastructure.Storage;

/// <summary>Store em memória para idempotência. Em Lambda, cada invocação pode ter memória nova; para produção use DynamoDB ou tabela.</summary>
public sealed class InMemorySentNotificationStore : ISentNotificationStore
{
    private static readonly ConcurrentDictionary<string, byte> Sent = new();

    public Task<bool> TryMarkSentAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Sent.TryAdd(messageId, 0));
    }

    public Task<bool> WasSentAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Sent.ContainsKey(messageId));
    }
}

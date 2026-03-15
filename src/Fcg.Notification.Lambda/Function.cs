using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Contracts.Messages;
using Fcg.Notification.Lambda.Observability;
using Microsoft.Extensions.Logging;

namespace Fcg.Notification.Lambda;

public sealed class Function
{
    private readonly INotificationHandler _handler;
    private readonly ILogger<Function> _logger;
    private readonly FcgMeters _meters;

    /// <summary>Receive count after which the message is considered poison (do not retry).</summary>
    private const int PoisonReceiveCountThreshold = 4;

    public Function(INotificationHandler handler, ILogger<Function> logger, FcgMeters meters)
    {
        _handler = handler;
        _logger = logger;
        _meters = meters;
    }

    /// <summary>Main handler: processes SQS batch, returns failures for retry, handles poison messages.</summary>
    public async Task<SQSBatchResponse> HandleSqsAsync(SQSEvent evnt, ILambdaContext context)
    {
        var batchFailures = new List<SQSBatchResponse.BatchItemFailure>();
        var sw = Stopwatch.StartNew();

        foreach (var record in evnt.Records)
        {
            var receiveCount = int.TryParse(record.Attributes?.GetValueOrDefault("ApproximateReceiveCount"), out var c) ? c : 1;
            var isPoison = receiveCount >= PoisonReceiveCountThreshold;

            if (isPoison)
            {
                _logger.LogError(
                    "Poison message discarded. {MessageId} {ReceiveCount} {BodyLength}",
                    record.MessageId, receiveCount, record.Body?.Length ?? 0);
                _meters.RecordException("PoisonMessageDiscarded");
                continue;
            }

            NotificationMessage? message = null;
            try
            {
                message = ParseMessage(record.Body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse SQS body. {MessageId}", record.MessageId);
                _meters.RecordException(ex.GetType().Name);
                batchFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
                continue;
            }

            if (message is null)
            {
                _logger.LogWarning("SQS body could not be parsed as NotificationMessage. {MessageId}", record.MessageId);
                batchFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
                continue;
            }

            if (string.IsNullOrWhiteSpace(message.MessageId))
                message.MessageId = record.MessageId;

            var itemSw = Stopwatch.StartNew();
            NotificationResult result;
            try
            {
                result = await ActivityRunContext.RunWithActivityAsync(
                    message.TraceId,
                    null,
                    message.CorrelationId,
                    "notification.handle",
                    async () =>
                    {
                        using (_logger.BeginScope(new Dictionary<string, object?>
                        {
                            [FcgLogPropertyNames.TraceId] = ObservabilityContext.GetCurrentTraceId() ?? "",
                            [FcgLogPropertyNames.SpanId] = ObservabilityContext.GetCurrentSpanId() ?? "",
                            [FcgLogPropertyNames.CorrelationId] = ObservabilityContext.GetCurrentCorrelationId() ?? "",
                            [FcgLogPropertyNames.MessageId] = message.MessageId,
                            [FcgLogPropertyNames.TemplateName] = message.TemplateName
                        }))
                        {
                            return await _handler.HandleAsync(message, CancellationToken.None).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler threw. {MessageId} {TraceId} {CorrelationId}", message.MessageId, message.TraceId, message.CorrelationId);
                _meters.RecordException(ex.GetType().Name);
                _meters.RecordEmailFailed();
                batchFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
                continue;
            }
            itemSw.Stop();

            if (result.Status == NotificationResultStatus.Sent)
                _meters.RecordEmailSent();
            else if (result.Status == NotificationResultStatus.SendFailed)
                _meters.RecordEmailFailed();

            _logger.LogInformation(
                "NotificationProcessed {MessageId} {TemplateName} {Status} {DurationMs} {CorrelationId} {TraceId}",
                message.MessageId, message.TemplateName, result.Status, itemSw.ElapsedMilliseconds,
                message.CorrelationId ?? "", message.TraceId ?? "");

            var retriable = IsRetriable(result);
            if (!retriable && result.Status != NotificationResultStatus.Sent && result.Status != NotificationResultStatus.SkippedDuplicate)
                _logger.LogWarning("Non-retriable result. {Status} {Error}", result.Status, result.ErrorMessage);

            if (retriable)
                batchFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = record.MessageId });
        }

        sw.Stop();
        _logger.LogInformation("Batch completed. {TotalMs}ms {FailureCount} failures", sw.ElapsedMilliseconds, batchFailures.Count);
        return new SQSBatchResponse(batchFailures);
    }

    private static NotificationMessage? ParseMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        return JsonSerializer.Deserialize<NotificationMessage>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static bool IsRetriable(NotificationResult result)
    {
        return result.Status switch
        {
            NotificationResultStatus.Sent => false,
            NotificationResultStatus.SkippedDuplicate => false,
            NotificationResultStatus.TemplateNotFound => false,
            NotificationResultStatus.ValidationFailed => false,
            NotificationResultStatus.SendFailed => true,
            _ => true
        };
    }
}

using System.Diagnostics;
using Fcg.Notification.Lambda.Telemetry;

namespace Fcg.Notification.Lambda.Observability;

/// <summary>Creates Activity from message trace/correlation (W3C). Uses LambdaActivitySource so spans are exported to X-Ray via OpenTelemetry.</summary>
public static class ActivityRunContext
{
    /// <summary>Starts an Activity as child of message trace (TraceId/SpanId). Sets correlation id tag. If traceId is null, starts a new trace.</summary>
    public static Activity? StartActivityFromMessage(string? traceId, string? spanId, string? correlationId, string activityName = "notification.handle")
    {
        ActivityContext parentContext = default;
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            try
            {
                var tid = ActivityTraceId.CreateFromString(traceId.AsSpan());
                var sid = string.IsNullOrWhiteSpace(spanId) ? default : ActivitySpanId.CreateFromString(spanId.AsSpan());
                parentContext = new ActivityContext(tid, sid, ActivityTraceFlags.None);
            }
            catch (FormatException)
            {
                // fall through with default parentContext
            }
        }

        var activity = LambdaActivitySource.Instance.StartActivity(activityName, ActivityKind.Consumer, parentContext);
        if (activity != null && !string.IsNullOrWhiteSpace(correlationId))
            activity.SetTag(ObservabilityContext.CorrelationIdTag, correlationId);
        return activity;
    }

    /// <summary>Runs the delegate inside an Activity created from the message. Disposes Activity on exit.</summary>
    public static async Task<T> RunWithActivityAsync<T>(string? traceId, string? spanId, string? correlationId, string activityName, Func<Task<T>> run)
    {
        using var activity = StartActivityFromMessage(traceId, spanId, correlationId, activityName);
        return await run().ConfigureAwait(false);
    }

    public static T RunWithActivity<T>(string? traceId, string? spanId, string? correlationId, string activityName, Func<T> run)
    {
        using var activity = StartActivityFromMessage(traceId, spanId, correlationId, activityName);
        return run();
    }
}

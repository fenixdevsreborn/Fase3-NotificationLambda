using System.Diagnostics;

namespace Fcg.Notification.Lambda.Observability;

/// <summary>Creates Activity from message trace/correlation (W3C). Use to continue trace from Payments API.</summary>
public static class ActivityRunContext
{
    /// <summary>Starts an Activity as child of message trace (TraceId/SpanId). Sets correlation id tag. If traceId is null, starts a new trace.</summary>
    public static Activity? StartActivityFromMessage(string? traceId, string? spanId, string? correlationId, string activityName = "process.message")
    {
        Activity? activity;
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            try
            {
                var tid = ActivityTraceId.CreateFromString(traceId.AsSpan());
                var sid = string.IsNullOrWhiteSpace(spanId) ? default : ActivitySpanId.CreateFromString(spanId.AsSpan());
                activity = new Activity(activityName).SetParentId(tid, sid, ActivityTraceFlags.None);
            }
            catch (FormatException)
            {
                activity = new Activity(activityName);
            }
        }
        else
        {
            activity = new Activity(activityName);
        }

        if (activity != null && !string.IsNullOrWhiteSpace(correlationId))
            activity.SetTag(ObservabilityContext.CorrelationIdTag, correlationId);
        activity?.Start();
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

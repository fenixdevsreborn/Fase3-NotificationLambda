using System.Diagnostics;

namespace Fcg.Notification.Lambda.Observability;

/// <summary>Observability context from Activity (trace/correlation). Used when processing SQS messages.</summary>
public static class ObservabilityContext
{
    /// <summary>Tag name for correlation id on Activity.</summary>
    public const string CorrelationIdTag = "correlation.id";

    public static string? GetCurrentTraceId() => Activity.Current?.TraceId.ToString();
    public static string? GetCurrentSpanId() => Activity.Current?.SpanId.ToString();
    public static string? GetCurrentCorrelationId() =>
        Activity.Current?.GetTagItem(CorrelationIdTag) as string;
}

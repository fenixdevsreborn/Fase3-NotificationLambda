namespace Fcg.Notification.Lambda.Observability;

/// <summary>Property names for structured logs. Include in log scope or message template.</summary>
public static class FcgLogPropertyNames
{
    public const string TraceId = "TraceId";
    public const string SpanId = "SpanId";
    public const string CorrelationId = "CorrelationId";
    public const string MessageId = "MessageId";
    public const string TemplateName = "TemplateName";
    public const string ExceptionType = "ExceptionType";
}

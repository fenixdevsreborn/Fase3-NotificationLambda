namespace Fcg.Notification.Lambda.Observability;

/// <summary>Metric names (snake_case) for Notification Lambda. Compatible with OpenTelemetry and CloudWatch.</summary>
public static class FcgMetricNames
{
    public const string EmailsSent = "emails.sent";
    public const string EmailsFailed = "emails.failed";
    public const string ExceptionsCount = "exceptions.count";

    public const string TagExceptionType = "exception.type";
}

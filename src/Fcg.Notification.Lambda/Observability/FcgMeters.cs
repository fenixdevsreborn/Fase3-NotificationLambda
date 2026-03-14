using System.Diagnostics.Metrics;

namespace Fcg.Notification.Lambda.Observability;

/// <summary>Meter facade for Notification Lambda: emails.sent, emails.failed, exceptions.count.</summary>
public sealed class FcgMeters
{
    private readonly Meter _meter;
    private readonly Counter<long> _emailsSent;
    private readonly Counter<long> _emailsFailed;
    private readonly Counter<long> _exceptionsCount;

    public FcgMeters(string meterName)
    {
        _meter = new Meter(meterName, "1.0.0");
        _emailsSent = _meter.CreateCounter<long>(FcgMetricNames.EmailsSent);
        _emailsFailed = _meter.CreateCounter<long>(FcgMetricNames.EmailsFailed);
        _exceptionsCount = _meter.CreateCounter<long>(FcgMetricNames.ExceptionsCount);
    }

    public Meter Meter => _meter;

    public void RecordEmailSent() => _emailsSent.Add(1);
    public void RecordEmailFailed() => _emailsFailed.Add(1);

    public void RecordException(string exceptionType)
    {
        _exceptionsCount.Add(1, new KeyValuePair<string, object?>(FcgMetricNames.TagExceptionType, exceptionType));
    }
}

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Fcg.Notification.Lambda.Observability;
using Xunit;

namespace Fcg.Notification.Lambda.Tests;

public class FcgMetersTests
{
    [Fact]
    public void RecordEmailSent_DoesNotThrow()
    {
        var sut = new FcgMeters("Fcg.Notification.Lambda.Test.Sent");
        sut.RecordEmailSent();
        sut.RecordEmailSent();
    }

    [Fact]
    public void RecordEmailFailed_DoesNotThrow()
    {
        var sut = new FcgMeters("Fcg.Notification.Lambda.Test.Failed");
        sut.RecordEmailFailed();
    }

    [Fact]
    public void RecordException_DoesNotThrow_AndIncrementsCounterWithTag()
    {
        var meterName = "Fcg.Notification.Lambda.Test." + Guid.NewGuid().ToString("N")[..8];
        var measurements = new ConcurrentBag<(string? name, long value, KeyValuePair<string, object?>? tag)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) =>
        {
            if (instrument.Meter.Name == meterName)
                listener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            KeyValuePair<string, object?>? tag = default;
            foreach (var t in tags)
            {
                if (t.Key == FcgMetricNames.TagExceptionType) { tag = t; break; }
            }
            measurements.Add((instrument.Name, value, tag));
        });
        listener.Start();

        var sut = new FcgMeters(meterName);
        sut.RecordException("InvalidOperationException");
        sut.RecordException("ArgumentException");
        listener.RecordObservableInstruments();

        var list = measurements.ToList();
        Assert.True(list.Count >= 2, "Expected at least 2 exception.count measurements");
        Assert.True(list.Exists(m => m.name == FcgMetricNames.ExceptionsCount && "InvalidOperationException".Equals(m.tag?.Value)),
            "Expected tag exception.type = InvalidOperationException");
    }
}

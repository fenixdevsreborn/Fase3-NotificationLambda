using System.Diagnostics;
using Fcg.Notification.Lambda.Observability;
using Xunit;

namespace Fcg.Notification.Lambda.Tests;

public class ActivityRunContextTests
{
    [Fact]
    public async Task RunWithActivityAsync_SetsActivityFromMessageTraceId()
    {
        var traceId = ActivityTraceId.CreateFromString("0123456789abcdef0123456789abcdef".AsSpan());
        var spanId = ActivitySpanId.CreateFromString("0123456789abcdef".AsSpan());
        string? capturedTraceId = null;
        string? capturedCorrelationId = null;

        await ActivityRunContext.RunWithActivityAsync(
            traceId.ToString(),
            spanId.ToString(),
            "corr-123",
            "test.activity",
            async () =>
            {
                capturedTraceId = Activity.Current?.TraceId.ToString();
                capturedCorrelationId = Activity.Current?.GetTagItem(ObservabilityContext.CorrelationIdTag) as string;
                await Task.Yield();
                return 42;
            });

        Assert.Equal(traceId.ToString(), capturedTraceId);
        Assert.Equal("corr-123", capturedCorrelationId);
    }

    [Fact]
    public void RunWithActivity_WhenTraceIdNull_StartsNewActivity()
    {
        int value = 0;
        ActivityRunContext.RunWithActivity(null, null, null, "test.sync", () =>
        {
            value = Activity.Current != null ? 1 : 0;
            return 1;
        });
        Assert.Equal(1, value);
    }
}

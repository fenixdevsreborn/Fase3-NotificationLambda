using System.Diagnostics;

namespace Fcg.Notification.Lambda.Telemetry;

/// <summary>ActivitySource for Notification Lambda. Use for custom spans if needed.</summary>
public static class LambdaActivitySource
{
    public const string Name = "Fcg.Notification.Lambda";

    private static readonly ActivitySource Source = new(Name, "1.0.0");

    public static ActivitySource Instance => Source;
}

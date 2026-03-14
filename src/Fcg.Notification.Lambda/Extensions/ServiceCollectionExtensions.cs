using Fcg.Notification.Lambda.Observability;
using Fcg.Notification.Lambda.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Notification.Lambda.Extensions;

/// <summary>DI extensions for Lambda observability and telemetry: FcgMeters, ActivitySource.</summary>
public static class ServiceCollectionExtensions
{
    public const string DefaultMeterName = "Fcg.Notification.Lambda";

    /// <summary>Adds FcgMeters and ActivitySource for Lambda. Call from Program.</summary>
    public static IServiceCollection AddLambdaObservability(this IServiceCollection services, string? meterName = null)
    {
        var name = string.IsNullOrWhiteSpace(meterName) ? DefaultMeterName : meterName;
        services.AddSingleton(new FcgMeters(name));
        services.AddSingleton(_ => LambdaActivitySource.Instance);
        return services;
    }
}

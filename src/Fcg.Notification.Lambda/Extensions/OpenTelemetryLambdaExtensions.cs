using Amazon.Lambda.Core;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Fcg.Notification.Lambda.Telemetry;

namespace Fcg.Notification.Lambda.Extensions;

/// <summary>Configura TracerProvider e MeterProvider para Lambda com ADOT (OTLP → X-Ray / CloudWatch).</summary>
public static class OpenTelemetryLambdaExtensions
{
    /// <summary>Endpoint OTLP quando rodando com ADOT Lambda extension. Só exporta se OTEL_EXPORTER_OTLP_ENDPOINT estiver definido.</summary>
    public const string DefaultOtlpEndpoint = "http://localhost:4318";

    private static TracerProvider? _tracerProvider;
    private static MeterProvider? _meterProvider;

    /// <summary>Nome do serviço (resource). Lido de OTEL_SERVICE_NAME ou padrão.</summary>
    public static string ServiceName { get; } =
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"))
            ? LambdaActivitySource.Name
            : Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")!.Trim();

    /// <summary>Constrói e retém TracerProvider e MeterProvider. Chamar uma vez no startup (Program). Só exporta se OTEL_EXPORTER_OTLP_ENDPOINT estiver definido.</summary>
    public static void ConfigureOpenTelemetry()
    {
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")?.Trim();
        var exportEnabled = !string.IsNullOrWhiteSpace(otlpEndpoint);
        if (!exportEnabled)
            return;
        var endpoint = otlpEndpoint!.TrimEnd('/');
        var endpointUri = new Uri(endpoint);
        var isGrpc = string.Equals(
            Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")?.Trim(),
            "grpc",
            StringComparison.OrdinalIgnoreCase);

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddAWSLambdaConfigurations(options =>
            {
                options.DisableAwsXRayContextExtraction = false;
                options.SetParentFromBatch = true;
            })
            .AddSource(LambdaActivitySource.Name)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = isGrpc ? endpointUri : new Uri(endpointUri, "v1/traces");
                opt.Protocol = isGrpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;
            })
            .Build();

        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(ServiceName)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = isGrpc ? endpointUri : new Uri(endpointUri, "v1/metrics");
                opt.Protocol = isGrpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;
            })
            .Build();
    }

    /// <summary>TracerProvider (pode ser null se OTLP não estiver configurado).</summary>
    public static TracerProvider? TracerProvider => _tracerProvider;

    /// <summary>MeterProvider (pode ser null se OTLP não estiver configurado).</summary>
    public static MeterProvider? MeterProvider => _meterProvider;

    /// <summary>Envolve o handler SQS com tracing (AWSLambdaWrapper.TraceAsync). Se TracerProvider for null, executa o handler sem wrapper.</summary>
    public static async Task<Amazon.Lambda.SQSEvents.SQSBatchResponse> TraceSqsHandlerAsync(
        Func<Amazon.Lambda.SQSEvents.SQSEvent, ILambdaContext, Task<Amazon.Lambda.SQSEvents.SQSBatchResponse>> handler,
        Amazon.Lambda.SQSEvents.SQSEvent evnt,
        ILambdaContext context)
    {
        if (_tracerProvider != null)
            return await AWSLambdaWrapper.TraceAsync(_tracerProvider, handler, evnt, context).ConfigureAwait(false);
        return await handler(evnt, context).ConfigureAwait(false);
    }

    /// <summary>Dispose dos providers. Chamar no shutdown se necessário (Lambda pode não chamar).</summary>
    public static void Shutdown()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}

using System.Text.Json;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Fcg.Notification.Application.Extensions;
using Fcg.Notification.Infrastructure.Extensions;
using Fcg.Notification.Lambda.Extensions;
using Microsoft.Extensions.Configuration;
using Function = Fcg.Notification.Lambda.Function;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});
services.AddLambdaObservability();
services.AddNotificationInfrastructure(configuration);
services.AddNotificationApplication();
services.AddSingleton<Function>();

var provider = services.BuildServiceProvider();

OpenTelemetryLambdaExtensions.ConfigureOpenTelemetry();

var handler = provider.GetRequiredService<Function>();
var serializer = new DefaultLambdaJsonSerializer(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

async Task<Amazon.Lambda.SQSEvents.SQSBatchResponse> WrappedHandler(Amazon.Lambda.SQSEvents.SQSEvent evnt, Amazon.Lambda.Core.ILambdaContext ctx)
    => await OpenTelemetryLambdaExtensions.TraceSqsHandlerAsync(handler.HandleSqsAsync, evnt, ctx).ConfigureAwait(false);

await LambdaBootstrapBuilder.Create<Amazon.Lambda.SQSEvents.SQSEvent, Amazon.Lambda.SQSEvents.SQSBatchResponse>(WrappedHandler, serializer)
    .Build()
    .RunAsync();

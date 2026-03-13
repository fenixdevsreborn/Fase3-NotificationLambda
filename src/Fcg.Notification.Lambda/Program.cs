using System.Text.Json;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Fcg.Notification.Application.Extensions;
using Fcg.Notification.Infrastructure.Extensions;
using Fcg.Shared.Observability;
using Microsoft.Extensions.Configuration;
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
services.AddSingleton(new FcgMeters("Fcg.Notification.Lambda"));
services.AddNotificationInfrastructure(configuration);
services.AddNotificationApplication();
services.AddSingleton<Fcg.Notification.Lambda.Function>();

var provider = services.BuildServiceProvider();
var handler = provider.GetRequiredService<Fcg.Notification.Lambda.Function>();
var serializer = new DefaultLambdaJsonSerializer(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

await LambdaBootstrapBuilder.Create<Amazon.Lambda.SQSEvents.SQSEvent, Amazon.Lambda.SQSEvents.SQSBatchResponse>(handler.HandleSqsAsync, serializer)
    .Build()
    .RunAsync();

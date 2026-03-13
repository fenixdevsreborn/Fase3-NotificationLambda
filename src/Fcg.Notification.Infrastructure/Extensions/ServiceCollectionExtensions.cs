using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Infrastructure.Email;
using Fcg.Notification.Infrastructure.Options;
using Fcg.Notification.Infrastructure.Payloads;
using Fcg.Notification.Infrastructure.Storage;
using Fcg.Notification.Infrastructure.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddSingleton<ITemplateResolver>(_ => new TemplateResolver(null));
        services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();
        services.AddSingleton<ITemplateModelBuilder, TemplateModelBuilder>();
        services.AddSingleton<IPayloadDeserializer, PayloadDeserializer>();
        services.AddSingleton<IPayloadValidator, PayloadValidator>();
        services.AddSingleton<ISentNotificationStore, InMemorySentNotificationStore>();

        var useSmtp = configuration.GetValue<bool>("Notification:UseSmtp");
        if (useSmtp)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, FakeEmailSender>();

        return services;
    }
}

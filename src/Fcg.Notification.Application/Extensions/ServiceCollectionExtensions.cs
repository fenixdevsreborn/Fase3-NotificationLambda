using Fcg.Notification.Application.Abstractions;
using Fcg.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Notification.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationHandler, NotificationHandler>();
        return services;
    }
}

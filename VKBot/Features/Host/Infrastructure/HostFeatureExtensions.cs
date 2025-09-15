using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Host.Services;
using VKBot.Features.Host.BackgroundServices;

namespace VKBot.Features.Host.Infrastructure;

public static class HostFeatureExtensions
{
    public static IServiceCollection AddHostFeature(this IServiceCollection services)
    {
        services.AddSingleton<MessageDeliveryBackgroundService>();
        services.AddSingleton<VkLongPollBackgroundService>();
        services.AddSingleton<AdminSyncBackgroundService>();

        return services;
    }
}
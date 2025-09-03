using VKBot.Features.Host.Services;

namespace VKBot.Features.Host.Infrastructure;

public static class HostFeatureExtensions
{
    public static IServiceCollection AddHostFeature(this IServiceCollection services)
    {
        services.AddHostedService<VkLongPollService>();
        services.AddHostedService<SenderService>();
        
        return services;
    }
}
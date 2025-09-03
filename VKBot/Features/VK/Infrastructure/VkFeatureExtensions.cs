using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Services;

namespace VKBot.Features.VK.Infrastructure;

public static class VkFeatureExtensions
{
    public static IServiceCollection AddVkFeature(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<IVkBot, VkBot>();
        
        return services;
    }
}
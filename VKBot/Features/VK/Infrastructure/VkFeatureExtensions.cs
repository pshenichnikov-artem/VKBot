using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Application.Services;

namespace VKBot.Features.VK.Infrastructure;

public static class VkFeatureExtensions
{
    public static IServiceCollection AddVkFeature(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<UpdateParseService>();
        services.AddScoped<IVkBot, VkBot>();
        
        return services;
    }
}
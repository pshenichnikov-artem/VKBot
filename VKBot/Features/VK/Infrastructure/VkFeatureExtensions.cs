using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKBot.Features.Host.Services;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Application.Services;
using VKBot.Features.VK.Application.Middleware;
using VKBot.Features.Host.BackgroundServices;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Infrastructure;

public static class VkFeatureExtensions
{
    public static IServiceCollection AddVkFeature(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddScoped<IVkBot, VkBot>();
        
        services.Scan(scan => scan
            .FromAssemblyOf<MiddlewareBase>()
            .AddClasses(classes => classes.AssignableTo<MiddlewareBase>())
            .AsSelf()
            .WithScopedLifetime());
            
        services.AddScoped<Pipeline>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Pipeline>>();
            var pipeline = new Pipeline(logger);
            
            // Порядок middleware
            pipeline.Use(provider.GetRequiredService<SendMessageMiddleware>());
            pipeline.Use(provider.GetRequiredService<ExceptionMiddleware>());
            //pipeline.Use(provider.GetRequiredService<AntiSpamMiddleware>());
            pipeline.Use(provider.GetRequiredService<ParseMessageMiddleware>());
            pipeline.Use(provider.GetRequiredService<CommandRouteMiddleware>());
            pipeline.Use(provider.GetRequiredService<AuthorizeMiddleware>());
            pipeline.Use(provider.GetRequiredService<StateMachineMiddleware>());
            
            return pipeline;
        });
        
        return services;
    }
}
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Infrastructure.Services;

namespace VKBot.Features.Core.Infrastructure
{
    public static class CoreFeatureExtensions
    {
        public static IServiceCollection AddCoreFeature(this IServiceCollection services)
        {
            services.AddScoped<IMessageProcessor, MessageProcessor>();
            services.AddScoped<ISessionService, SessionService>();
            
            return services;
        }
    }
}
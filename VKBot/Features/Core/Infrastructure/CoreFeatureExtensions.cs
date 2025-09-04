using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Infrastructure.Services;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Core.Infrastructure
{
    public static class CoreFeatureExtensions
    {
        public static IServiceCollection AddCoreFeature(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));
            
            services.AddSingleton<IConnectionMultiplexer>(provider =>
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
            
            services.AddScoped<IMessageProcessor, MessageProcessor>();
            services.AddScoped<ISessionService, SessionService>();
            
            return services;
        }
    }
}
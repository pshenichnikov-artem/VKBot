using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Core.Infrastructure
{
    public static class CoreFeatureExtensions
    {
        public static IServiceCollection AddCoreFeature(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));
            
            services.AddMemoryCache();
            services.AddScoped<IStateFactory, StateFactory>();
            services.AddScoped<IStateMachineFactory, StateMachineFactory>();
            services.AddSingleton<IStateDiscoveryService, StateDiscoveryService>();
            services.AddScoped<UserNotificationService>();
            
            // Регистрация всех BaseState через Scrutor
            services.Scan(scan => scan
                .FromAssemblyOf<BaseState>()
                .AddClasses(classes => classes.AssignableTo<BaseState>())
                .AsSelf()
                .WithScopedLifetime());
            
            return services;
        }
    }
}
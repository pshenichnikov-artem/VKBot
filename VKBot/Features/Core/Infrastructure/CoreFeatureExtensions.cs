using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Application.Services.Factory;
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
            services.AddScoped<MessageContentService>();
            
            // Регистрация провайдеров контента
            services.Scan(scan => scan
                .FromAssemblyOf<IMessageContentProvider>()
                .AddClasses(classes => classes.AssignableTo<IMessageContentProvider>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
                
            // Регистрация Excel провайдеров
            services.Scan(scan => scan
                .FromAssemblyOf<IExcelReportProvider>()
                .AddClasses(classes => classes.AssignableTo<IExcelReportProvider>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            
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
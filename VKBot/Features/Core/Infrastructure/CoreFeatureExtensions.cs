using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Handlers;
using VKBot.Features.Core.Infrastructure.Services;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Core.Infrastructure;

public static class CoreFeatureExtensions
{
    public static IServiceCollection AddCoreFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(provider =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")));

        // Services
        services.AddScoped<ISessionService, SessionService>();

        // Commands
        services.Scan(scan => scan
            .FromAssemblyOf<IVKCommand>()
            .AddClasses(classes => classes.AssignableTo<IVKCommand>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Steps
        services.Scan(scan => scan
        .FromAssemblyOf<IVKCommand>()
        .AddClasses(c => c
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IVKCommandStep<>))))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

        services.AddScoped<ICommandHandler, CommandHandler>();
        
        return services;
    }
}
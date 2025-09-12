using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Enums;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VKBot.Features.VK.Application.Exceptions;

namespace VKBot.Features.VK.Application.Middleware;

public class CommandRouteMiddleware : MiddlewareBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _memoryCache;

    public CommandRouteMiddleware(IServiceProvider serviceProvider, IMemoryCache memoryCache, ILogger<CommandRouteMiddleware> logger) : base(logger)
    {
        _serviceProvider = serviceProvider;
        _memoryCache = memoryCache;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        
        var cacheKey = $"state_machine_{context.Message?.FromId}";
        var hasActiveState = _memoryCache.TryGetValue(cacheKey, out _);

        if (hasActiveState)
        {
            await next();
            return;
        }

        BaseState? foundState = null;

        // Сначала ищем по payload (приоритетнее)
        if (context.Message?.Payload.HasValue == true && 
            context.Message.Payload.Value.TryGetProperty("type", out var typeElement) &&
            Enum.TryParse<PayloadType>(typeElement.GetString(), true, out var payloadType))
        {
            foundState = FindStateByPayloadType(payloadType);
        }

        if (foundState == null && !string.IsNullOrEmpty(context.Message?.Text))
        {
            foundState = FindStateByCommand(context.Message.Text.ToLower().Trim());
        }

        context.FoundState = foundState;
        

        
        if (foundState == null)
        {
            _logger.LogInformation("Команда не найдена: {Text}", context.Message?.Text);
            throw new CommandNotFoundException();
        }

        _logger.LogInformation("Найдена команда: {Command}", foundState.GetType().Name);
        await next();
    }

    private BaseState? FindStateByPayloadType(PayloadType payloadType)
    {
        var stateType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .FirstOrDefault(t => t.GetCustomAttribute<StateAttribute>()?.PayloadType == payloadType);

        return stateType != null ? (BaseState)_serviceProvider.GetRequiredService(stateType) : null;
    }

    private BaseState? FindStateByCommand(string command)
    {
        var stateType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .FirstOrDefault(t => string.Equals(t.GetCustomAttribute<StateAttribute>()?.Command, command, StringComparison.OrdinalIgnoreCase)
            && t.GetCustomAttribute<StateAttribute>()?.IsEntryState == true);
        
        return stateType != null ? (BaseState)_serviceProvider.GetRequiredService(stateType) : null;
    }
}
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

        List<BaseState> foundStates = new();

        // Сначала ищем по payload (приоритетнее)
        if (context.Message?.Payload.HasValue == true && 
            context.Message.Payload.Value.TryGetProperty("type", out var typeElement) &&
            Enum.TryParse<PayloadType>(typeElement.GetString(), true, out var payloadType))
        {
            foundStates = FindStateByPayloadType(payloadType);
        }

        if (foundStates.Count == 0 && !string.IsNullOrEmpty(context.Message?.Text))
        {
            foundStates = FindStateByCommand(context.Message.Text.ToLower().Trim());
        }
        
        if (foundStates.Count == 0)
        {
            _logger.LogInformation("Команда не найдена: {Text}", context.Message?.Text);
            throw new CommandNotFoundException();
        }

        context.FoundStates = foundStates;
        context.FoundState = foundStates.Count == 1 ? foundStates[0] : null;
        
        _logger.LogInformation("Найдено состояний: {Count}", foundStates.Count);
        await next();
    }

    private List<BaseState> FindStateByPayloadType(PayloadType payloadType)
    {
        var stateTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<StateAttribute>()?.PayloadType == payloadType);

        return stateTypes.Select(t => (BaseState)_serviceProvider.GetRequiredService(t)).ToList();
    }

    private List<BaseState> FindStateByCommand(string command)
    {
        var stateTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .Where(t => string.Equals(t.GetCustomAttribute<StateAttribute>()?.Command, command, StringComparison.OrdinalIgnoreCase)
            && t.GetCustomAttribute<StateAttribute>()?.IsEntryState == true);
        
        return stateTypes.Select(t => (BaseState)_serviceProvider.GetRequiredService(t)).ToList();
    }
}
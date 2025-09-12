using VKBot.Features.VK.Domain.Models;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public class StateMachineMiddleware : MiddlewareBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;

    public StateMachineMiddleware(IMemoryCache memoryCache, IServiceProvider serviceProvider, ILogger<StateMachineMiddleware> logger) : base(logger)
    {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        _logger.LogInformation("→ INVOKE начало - обработка сообщения от пользователя {UserId}", context.Message?.FromId);
        
        if (context.Message?.FromId != null)
        {
            var userMessage = new UserMessage
            {
                UserId = context.Message.FromId,
                MessageId = context.Message.Id,
                Text = context.Message.Text,
                Payload = context.Message.Payload != null ? 
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(context.Message.Payload.ToString()!) : null
            };

            var cacheKey = $"state_machine_{context.Message.FromId}";
            var hadActiveState = _memoryCache.TryGetValue(cacheKey, out _);
            
            _logger.LogInformation("Активное состояние: {HasState}, Новое состояние: {NewState}", 
                hadActiveState, context.FoundState?.GetType().Name ?? "null");

            var stateMachine = StateMachine.GetOrCreate(context.Message.FromId, _memoryCache, _serviceProvider);
            var result = await stateMachine.ProcessMessage(userMessage, context.FoundState);

            foreach (var message in result.Messages)
            {
                if (!string.IsNullOrEmpty(message.Text) || message.Keyboard != null || message.Attachments.Count > 0 || message.IsForwardMessage)
                {
                    context.Results.Add(new VkResult
                    {
                        Text = message.Text,
                        UserId = context.Message.FromId,
                        Keyboard = message.Keyboard,
                        Attachments = message.Attachments,
                        IsForwardMessage = message.IsForwardMessage,
                        ForwardMessageId = message.ForwardMessageId,
                        ReplyToMessageId = message.ReplyToMessageId
                    });
                }
            }

            var hasActiveStateAfter = _memoryCache.TryGetValue(cacheKey, out _);
            if (!hasActiveStateAfter)
            {
                _logger.LogInformation("Состояние завершено, показываем кнопки");
                context.ShouldShowKeyboard = true;
            }
        }
        
        await next();
        _logger.LogInformation("← INVOKE завершено - сообщение обработано");
    }
}
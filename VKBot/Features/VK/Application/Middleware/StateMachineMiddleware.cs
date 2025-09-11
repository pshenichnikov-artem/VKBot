using VKBot.Features.VK.Domain.Models;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace VKBot.Features.VK.Application.Middleware;

public class StateMachineMiddleware : MiddlewareBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceProvider _serviceProvider;

    public StateMachineMiddleware(IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
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

            var stateMachine = StateMachine.GetOrCreate(context.Message.FromId, _memoryCache, _serviceProvider);
            var result = await stateMachine.ProcessMessage(userMessage, context.FoundState);
            
            if (!string.IsNullOrEmpty(result.Text) || result.Keyboard != null || result.Attachments.Count > 0)
            {
                context.Results.Add(new VkResult
                {
                    Text = result.Text,
                    UserId = context.Message.FromId,
                    Keyboard = result.Keyboard,
                    Attachments = result.Attachments
                });
            }
        }
        
        await next();
    }
}
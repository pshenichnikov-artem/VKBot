using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Services;

public class MessageContentService
{
    private readonly IEnumerable<IMessageContentProvider> _providers;

    public MessageContentService(IEnumerable<IMessageContentProvider> providers)
    {
        _providers = providers;
    }

    public async Task<StateResult> GenerateMessageContent(Message message)
    {
        if (string.IsNullOrEmpty(message.Payload))
            return new StateResult("У вас новое сообщение", StateAction.End);

        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
            if (payload.TryGetProperty("type", out var typeElement))
            {
                var messageType = typeElement.GetString();
                var provider = _providers.FirstOrDefault(p => p.GetMessageType() == messageType);
                
                if (provider != null)
                {
                    return await provider.GenerateMessageContent(message);
                }
            }
        }
        catch
        {
            // Fallback to default message
        }

        return new StateResult("У вас новое сообщение", StateAction.End);
    }
}
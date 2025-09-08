using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.Services.MessageContentProviders;

public class EventContentProvider : IMessageContentProvider
{
    public string GetMessageType() => "event";

    public async Task<StateResult> GenerateMessageContent(Message message)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload!);
        var title = payload.GetProperty("title").GetString();
        var deadline = payload.GetProperty("deadline").GetString();

        var text = $"📢 Новое событие: {title}\n\n" +
                   $"⏰ Время для ответа до: {DateTime.Parse(deadline!):dd.MM.yyyy HH:mm}\n\n" +
                   $"Пожалуйста, ответьте на это сообщение.";

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Ответить", VkButtonColor.Primary, payload: $"{{\"type\":\"event_response\",\"messageId\":{message.Id}}}");

        return StateResult.Success(text, StateAction.End, keyboard: keyboard);
    }
}
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
    public string GetMessageType() => PayloadType.Event.ToString();

    public async Task<StateResult> GenerateMessageContent(Message message)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload!);
        var title = payload.GetProperty("title").GetString();
        var deadline = payload.GetProperty("deadline").GetString();

        var text = $"📢 Пришло новое событие: {title}\n\n" +
                   $"⏰ Время для ответа до: {DateTime.Parse(deadline!).AddHours(3):dd.MM.yyyy HH:mm} МСК\n\n" +
                   $"Посмотрите событие и ответьте на него.";

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("События", VkButtonColor.Primary);

        return new StateResult(text, StateAction.End, keyboard: keyboard);
    }
}
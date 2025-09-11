using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.Services.MessageContentProviders;

public class AlertContentProvider : IMessageContentProvider
{
    public string GetMessageType() => PayloadType.Alert.ToString();

    public async Task<StateResult> GenerateMessageContent(Message message)
    {
        var text = "🚨 ВОЗДУШНАЯ ТРЕВОГА!\n\n" +
                   "Немедленно укройтесь в безопасном месте.\n" +
                   "Ответьте ТОЛЬКО ЧИСЛОМ - количество студентов в укрытии.\n\n" +
                   "Время на ответ: 2 часа.";

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Ответить", VkButtonColor.Negative, payload: $"{{\"type\":\"{PayloadType.AlertResponse}\",\"messageId\":{message.Id}}}");

        return new StateResult(text, StateAction.End, keyboard: keyboard);
    }
}
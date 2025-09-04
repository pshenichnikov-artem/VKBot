using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Models;

namespace VKBot.Features.Core.Application.Commands
{
    public class TestStateAdmin : StateDecorator
    {
        protected override Dictionary<(string command, UserRole role), Type> Transitions => new();

        protected override bool IsLast => true;

        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var keyboard = new VkKeyboard
            {
                OneTime = false,
                Inline = true,
                Buttons = new List<List<VkButton>>
                {
                    new List<VkButton>
                    {
                        new VkButton
                        {
                            Action = new VkButtonAction
                            {
                                Type = "text",
                                Label = "Да",
                                Payload = "{\"button\": \"yes\"}"
                            },
                            Color = "positive"
                        },
                        new VkButton
                        {
                            Action = new VkButtonAction
                        {
                            Type = "text",
                            Label = "Нет",
                            Payload = "{\"button\": \"no\"}"
                        },
                        Color = "negative"
                        }
                    },
                    new List<VkButton>
                    {
                        new VkButton {
                            Action = new VkButtonAction
                            {
                                Type = "text",
                                Label = "Второй ряд",
                                Payload = "{\"button\": \"second\"}"
                            },
                            Color = "secondary"
                        }
                    }
                }
            };

            //photo-227681680_457239022
            List<StateAttachment>? attachments = new List<StateAttachment>
            {
                new StateAttachment
                {
                    Type = "photo",
                    OwnerId = -227681680,
                    MediaId = 457239022
                }
            };

            return Task.FromResult(StateResult.Success("Тестовая команда", message.MessageId, attachments, keyboard));
        }
    }
}

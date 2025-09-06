using VKBot.Features.Core.Application.Commands.Admin.Confirmation;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Models;

namespace VKBot.Features.Core.Application.Commands.User
{
    public class TestState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("Тест1", UserRole.Student), typeof(TestState) },
            { ("Тест2", UserRole.Student), typeof(TestState) },
            { ("/test3", UserRole.Student), typeof(TestState) }
        };

        protected override bool IsLast => true;

        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var keyboard = CreateKeyboardFromCommands(message.UserId);
            
            //photo-227681680_457239023
            List<StateAttachment>? attachments = new List<StateAttachment>
            {
                new StateAttachment
                {
                    Type = "photo",
                    OwnerId = -227681680,
                    MediaId = 457239023
                }
            };

            return Task.FromResult(StateResult.Success("Тестовая команда", message.MessageId, attachments, keyboard));
        }
    }
}

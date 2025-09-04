using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands
{
    public class SaveStreamState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", UserRole.Admin), typeof(SendMessageState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            session.Data["sendType"] = "stream";
            session.Data["stream"] = message.Text.Trim();
            return Task.FromResult(StateResult.Success("Введите ваше сообщение:"));
        }
    }
}
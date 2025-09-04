using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands
{
    public class SendMessageState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole role), Type> Transitions => new()
        {
            { ("*", UserRole.Admin), typeof(SendCompleteState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            session.Data["group"] = message.Text;
            return Task.FromResult(StateResult.Success("Введите ваше сообщение"));
        }
    }
}
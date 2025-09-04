using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
namespace VKBot.Features.Core.Application.Commands
{
    public class SendCompleteState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Сообщение отправлено"));
        }
    }
}
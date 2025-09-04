using VKBot.Features.Core.Domain.Interfaces;

using VKBot.Features.Core.Domain.Models;
namespace VKBot.Features.Core.Application.Commands
{
    public class SendCompleteState : StateDecorator
    {
        protected override Dictionary<string, Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<string?> ExecuteAsync(UserMessage message, UserSession session)
        {
            return "Сообщение отправлено";
        }
    }
}
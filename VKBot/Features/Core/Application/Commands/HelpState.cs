using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands
{
    public class HelpState : StateDecorator
    {
        protected override Dictionary<string, Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<string?> ExecuteAsync(UserMessage message, UserSession session)
        {
            return "Справка по командам:\n/start - начать работу\n/send - отправить сообщение\n/help - показать эту справку";
        }
    }
}
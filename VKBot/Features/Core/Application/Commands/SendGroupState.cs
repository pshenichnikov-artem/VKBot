using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands
{
    public class SendGroupState : StateDecorator
    {
        protected override Dictionary<string, Type> Transitions => new()
        {
            { "*", typeof(SendMessageState) }
        };
        
        protected override async Task<string?> ExecuteAsync(UserMessage message, UserSession session)
        {
            return "Выберите группу";
        }
    }
}
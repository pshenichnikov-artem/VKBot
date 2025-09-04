using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;

namespace VKBot.Features.Core.Application.Commands
{
    public class SendMessageState : StateDecorator
    {
        protected override Dictionary<string, Type> Transitions => new()
        {
            { "*", typeof(SendCompleteState) }
        };
        
        protected override async Task<string?> ExecuteAsync(UserMessage message, UserSession session)
        {
            session.SetData("group", message.Text);
            return "Введите ваше сообщение";
        }
    }
}
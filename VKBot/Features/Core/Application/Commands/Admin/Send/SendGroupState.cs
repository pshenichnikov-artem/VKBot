using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands
{
    public class SendGroupState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("Все", UserRole.Admin), typeof(SendMessageState) },
            { ("Потоку", UserRole.Admin), typeof(EnterStreamState) },
            { ("Группам", UserRole.Admin), typeof(EnterGroupsState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Кому отправить сообщение?\n\nНажмите:\n- 'Все' - всем студентам\n- 'Потоку' - конкретному потоку\n- 'Группам' - конкретным группам"));
        }
    }
}
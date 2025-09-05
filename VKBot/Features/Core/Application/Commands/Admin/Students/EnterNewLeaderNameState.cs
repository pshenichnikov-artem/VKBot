using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin.Students
{
    public class EnterNewLeaderNameState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", UserRole.Admin), typeof(ChangeLeaderState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            session.Data["selectedGroup"] = message.Text.Trim();
            return Task.FromResult(StateResult.Success("Введите новое ФИО старосты:"));
        }
    }
}
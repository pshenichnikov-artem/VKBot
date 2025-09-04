using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Application.Commands.Admin.Confirmation;

namespace VKBot.Features.Core.Application.Commands
{
    public class UnansweredStudentsState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", UserRole.Admin), typeof(PendingMessagesState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Неподтвержденные студенты:\n(здесь будет список студентов для подтверждения)"));
        }
    }
}
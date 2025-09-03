using VKBot.Features.Core.Application.Commands;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.CommandSteps
{
    //TODO Убрать пример
    public class StartCommandStepFirst : IVKCommandStep<StartCommand>
    {
        public int StepNumber => 1;

        public Task<CommandStepResult> ExecuteAsync(long vkUserId, string input, UserSession session)
        {
            throw new NotImplementedException();
        }
    }
}

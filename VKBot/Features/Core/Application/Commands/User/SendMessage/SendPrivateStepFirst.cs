using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands.User.SendMessage
{
    public class SendPrivateStepFirst : IVKCommandStep<SendPrivateCommand>
    {
        public int StepNumber => 0;

        public Task<CommandStepResult> ExecuteAsync(long vkUserId, string input, UserSession session)
        {
            //TODO логика отправки сообщения
            var result = new CommandStepResult("Сообщение отправлено", true);
            return Task.FromResult(result);
        }
    }
}

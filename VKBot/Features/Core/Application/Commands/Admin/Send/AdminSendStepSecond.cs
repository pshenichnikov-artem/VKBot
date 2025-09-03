using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands.Admin.Send
{
    public class AdminSendStepSecond : IVKCommandStep<AdminSendCommand>
    {
        public int StepNumber => 1;

        public Task<CommandStepResult> ExecuteAsync(long vkUserId, string input, UserSession session)
        {
            //Принять сообщение и сохранить в бд

            return Task.FromResult(new CommandStepResult("Сообщение отправлено", true));
        }
    }
}

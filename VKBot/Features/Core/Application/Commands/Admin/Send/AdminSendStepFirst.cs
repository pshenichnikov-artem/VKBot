using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands.Admin.Send
{
    public class AdminSendStepFirst : IVKCommandStep<AdminSendCommand>
    {
        public int StepNumber => 0;

        public Task<CommandStepResult> ExecuteAsync(long vkUserId, string input, UserSession session)
        {
            //Валидация групп
            var result = new CommandStepResult("Введите текст сообщения:", true);
            return Task.FromResult(result);
        }
    }
}

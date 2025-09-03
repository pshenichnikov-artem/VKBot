using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin.Send
{
    public class AdminSendCommand(IEnumerable<IVKCommandStep> steps) : IVKCommand
    {
        public IEnumerable<UserRole> UserRoles => [UserRole.Admin];

        public string CommandName => "send";

        public string Description => "Отправить сообщение группам";

        public IReadOnlyCollection<IVKCommandStep> Steps => steps
            .OrderBy(s => s.StepNumber)
            .ToList();

        public Task<string> ExecuteAsync(long vkUserId, string[] args)
        {
            //TODO получить группы
            string result = "Выберите группы:\n";

            return Task.FromResult(result);
        }
    }
}

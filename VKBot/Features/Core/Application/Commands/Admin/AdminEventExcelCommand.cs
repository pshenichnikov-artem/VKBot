using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin
{
    public class AdminEventExcelCommand : IVKCommand
    {
        public IEnumerable<UserRole> UserRoles => [UserRole.Admin];

        public string CommandName => "excel";

        public string Description => "Получить статистику ответов старост";

        public IReadOnlyCollection<IVKCommandStep> Steps => Array.Empty<IVKCommandStep>();

        public Task<string> ExecuteAsync(long vkUserId, string[] args)
        {
            //Выдать excel
            return Task.FromResult(string.Empty);
        }
    }
}

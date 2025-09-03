using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin
{
    public class AdminSendAlarmCommand : IVKCommand
    {
        public IEnumerable<UserRole> UserRoles => [UserRole.Admin];

        public string CommandName => "alarm";

        public string Description => "Сообщить всем о воздушной тревоге";

        public IReadOnlyCollection<IVKCommandStep> Steps => Array.Empty<IVKCommandStep>();

        public Task<string> ExecuteAsync(long vkUserId, string[] args)
        {
            //TODO получить всех пользователей и отправить им заготовку
            return Task.FromResult("Сообщение отправлено");
        }
    }
}

using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User.SendMessage
{
    public class SendPrivateCommand : IVKCommand
    {
        public IEnumerable<UserRole> UserRoles => [UserRole.Student];

        public string CommandName => "msg";

        public string Description => "Задать вопрос администратору";

        public IReadOnlyCollection<IVKCommandStep> Steps => throw new NotImplementedException();

        public Task<string> ExecuteAsync(long vkUserId, string[] args)
        {
            return Task.FromResult("Введите ваше сообщение:");
        }
    }
}

using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin;

public class AdminStartCommand : IVKCommand
{
    public IEnumerable<UserRole> UserRoles => [UserRole.Admin];
    public string CommandName => "start";
    public string Description => "";

    public IReadOnlyCollection<IVKCommandStep> Steps { get; } = Array.Empty<IVKCommandStep>();

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        return "Добро пожаловать, администратор! Используйте /adminhelp для списка команд.";
    }
}
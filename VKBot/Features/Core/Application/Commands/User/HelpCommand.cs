using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User;

public class HelpCommand(IEnumerable<IVKCommand> vKCommands) : IVKCommand
{
    public string CommandName => "help";
    public string Description => "Список доступных команд";

    public IReadOnlyCollection<IVKCommandStep> Steps { get; } = Array.Empty<IVKCommandStep>();

    public IEnumerable<UserRole> UserRoles => [UserRole.Student];

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        string result = "Доступные команды:\n";
        foreach (var commands in vKCommands.Where(c => c.UserRoles.Contains(UserRole.Student)))
        {
            result += $"{commands.CommandName} - {commands.Description}\n";
        }
        return result;
    }
}
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin;

public class AdminHelpCommand(IEnumerable<IVKCommand> vKCommands) : IVKCommand
{
    public string CommandName => "help";
    public string Description => "Показать команды администратора";

    public IReadOnlyCollection<IVKCommandStep> Steps { get; } = Array.Empty<IVKCommandStep>();

    public IEnumerable<UserRole> UserRoles => [UserRole.Admin];

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        string result = "Команды администратора:\n";
        foreach (var command in vKCommands.Where(c => c.UserRoles.Contains(UserRole.Admin)))
        {
            result += $"{command.CommandName} - {command.Description}\n";
        }
        return result;
    }
}
using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands;

public class HelpCommand(IEnumerable<IVKCommand> vKCommands) : IVKCommand
{
    public string CommandName => "help";
    public string Description => string.Empty;

    public IReadOnlyCollection<IVKCommandStep> Steps { get; } = Array.Empty<IVKCommandStep>();

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        //TODO проверка на роль
        string result = "Доступные команды:\n";
        foreach (var commands in vKCommands)
        {
            result += $"{commands.CommandName} - {commands.Description}\n";
        }
        return result;
    }
}
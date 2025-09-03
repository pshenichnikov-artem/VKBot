using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User;

public class StartCommand(IEnumerable<IVKCommandStep<StartCommand>> steps) : IVKCommand
{
    public IEnumerable<UserRole> UserRoles => [UserRole.Student];
    public string CommandName => "start";
    public string Description => "Начало работы бота";

    public IReadOnlyCollection<IVKCommandStep> Steps { get; init; } = Array.Empty<IVKCommandStep>();

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        //TODO сделать номрально
        return "Добро пожаловать! Используйте /help для списка команд.";
    }
}
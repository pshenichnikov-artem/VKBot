using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands;

public class StartCommand(IEnumerable<IVKCommandStep<StartCommand>> steps) : IVKCommand
{
    public string CommandName => "start";
    public string Description => "Начало работы бота";

    public IReadOnlyCollection<IVKCommandStep> Steps { get; init; } = steps
            .OrderBy(s => s.StepNumber)
            .ToList();

    public async Task<string> ExecuteAsync(long vkUserId, string[] args)
    {
        //TODO сделать номрально
        return "Добро пожаловать! Используйте /help для списка команд.";
    }
}
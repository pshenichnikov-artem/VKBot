using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Domain.Interfaces;

/// <summary>
/// НЕ РЕАЛИЗОВЫВАТЬ ЭТОТ ИНТЕРФЕЙС, А ИСПОЛЬЗОВАТЬ GENERIC ВЕРСИЮ
/// </summary>
public interface IVKCommandStep
{
    /// <summary>
    /// Первый шаг начинается с 0
    /// </summary>
    int StepNumber { get; }
    Task<CommandStepResult> ExecuteAsync(long vkUserId, string input, UserSession session);
}

public interface IVKCommandStep<TCommand> : IVKCommandStep
    where TCommand : IVKCommand
{
}

public class CommandStepResult
{
    public CommandStepResult(string message, bool isCompleted)
    {
        Message = message;
        IsCompleted = isCompleted;
    }

    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Application.Handlers;

public class CommandHandler : ICommandHandler
{
    private readonly IEnumerable<IVKCommand> _commands;
    private readonly ISessionService _sessionService;

    public CommandHandler(IEnumerable<IVKCommand> commands, ISessionService sessionService)
    {
        _commands = commands;
        _sessionService = sessionService;
    }

    public async Task<string> HandleAsync(long vkUserId, string vkUserMessage)
    {

        var parts = vkUserMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0][1..];
        var args = parts.Skip(1).ToArray();
        if(commandName == "cancel")
        {
            await _sessionService.DeleteSessionAsync(vkUserId);
            return "Команда сброшена";
        }

        var userSession = await _sessionService.GetSessionAsync(vkUserId);

        IVKCommand? command;
        if (userSession != null)
        {
            command = _commands.FirstOrDefault(c => c.CommandName.Equals(userSession.CommandName, StringComparison.OrdinalIgnoreCase));
            if (command != null)
            {
                var currentStep = command.Steps.FirstOrDefault(s => s.StepNumber == userSession.Step);
                if (currentStep != null)
                {
                    var stepResult = await currentStep.ExecuteAsync(vkUserId, vkUserMessage, userSession);

                    if (stepResult.IsCompleted)
                    {
                        var maxStep = command.Steps.Max(s => s.StepNumber);
                        if(currentStep.StepNumber < maxStep)
                        {
                            userSession.Data.Add(userSession.Step.ToString(), vkUserMessage);
                            userSession.Step++;
                            await _sessionService.SetSessionAsync(userSession);
                        }
                        else
                        {
                            await _sessionService.DeleteSessionAsync(userSession.VkUserId);
                        }
                    }
                    
                    return stepResult.Message;
                }
            }
        }
        
        if (!vkUserMessage.StartsWith('/'))
            return "Неизвестная команда";

        command = _commands.FirstOrDefault(c => c.CommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        
        if (command != null)
        {
            var result = await command.ExecuteAsync(vkUserId, args);
            
            if (command.Steps.Any())
            {
                var newSession = new UserSession
                {
                    VkUserId = vkUserId,
                    CommandName = commandName,
                    Step = 1
                };
                await _sessionService.SetSessionAsync(newSession);
            }
            
            return result;
        }
        
        return $"Команда '/{commandName}' не найдена";
    }
}

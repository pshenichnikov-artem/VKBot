using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.Core.Application.Services
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<MessageProcessor> _logger;
        
        public MessageProcessor(ISessionService sessionService, ILogger<MessageProcessor> logger, IServiceProvider serviceProvider)
        {
            _sessionService = sessionService;
            _logger = logger;
            StateDecorator.ServiceProvider = serviceProvider;
        }
        
        public async Task<StateResult> ProcessMessageAsync(UserMessage message)
        {
            var session = await _sessionService.GetSessionAsync(message.UserId) ?? new UserSession { UserId = message.UserId };
            
            var currentStateType = session.GetCurrentState() ?? typeof(BaseState);
            _logger.LogInformation("Пользователь {UserId} в состоянии {State} отправил сообщение: {Message}", 
                message.UserId, currentStateType.Name, message.Text);
            
            var currentState = (StateDecorator)StateDecorator.ServiceProvider.GetService(currentStateType)!;
            
            var (nextState, result) = await currentState.ProcessAsync(message, session);
            
            if (nextState == currentState)
            {
                await _sessionService.SetSessionAsync(session);
            }
            else if (nextState != null)
            {
                _logger.LogInformation("Пользователь {UserId} перешел из {FromState} в {ToState}", 
                    message.UserId, currentStateType.Name, nextState.GetType().Name);
                session.SetCurrentState(nextState.GetType());
                await _sessionService.SetSessionAsync(session);
            }
            else
            {
                _logger.LogInformation("Сессия пользователя {UserId} завершена из состояния {State}", 
                    message.UserId, currentStateType.Name);
                await _sessionService.DeleteSessionAsync(message.UserId);
            }
            
            return result;
        }

    }
}
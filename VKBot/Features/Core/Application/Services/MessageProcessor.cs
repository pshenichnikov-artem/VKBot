using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;

namespace VKBot.Features.Core.Application.Services
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ISessionService _sessionService;
        
        public MessageProcessor(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }
        
        public async Task<string?> ProcessMessageAsync(UserMessage message)
        {
            var session = await _sessionService.GetSessionAsync(message.UserId) ?? new UserSession { UserId = message.UserId };
            
            var currentStateType = session.GetCurrentState() ?? typeof(BaseState);
            var currentState = (StateDecorator)Activator.CreateInstance(currentStateType)!;
            
            var nextState = await currentState.ProcessAsync(message, session);
            
            if (nextState != null)
            {
                session.SetCurrentState(nextState.GetType());
                await _sessionService.SetSessionAsync(session);
            }
            else
            {
                await _sessionService.DeleteSessionAsync(message.UserId);
            }
            
            return session.GetLastResponse();
        }

    }
}
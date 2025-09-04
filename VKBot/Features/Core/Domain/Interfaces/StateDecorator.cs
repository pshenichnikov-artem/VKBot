using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;

namespace VKBot.Features.Core.Domain.Interfaces
{
    public abstract class StateDecorator
    {
        protected abstract Dictionary<string, Type> Transitions { get; }
        protected virtual bool IsLast => false;
        
        protected virtual async Task<string?> ExecuteAsync(UserMessage message, UserSession session) => null;
        
        public async Task<StateDecorator?> ProcessAsync(UserMessage message, UserSession session)
        {
            if (message.Text == "/cancel")
                return new BaseState();
                
            if (Transitions.ContainsKey(message.Text) || Transitions.ContainsKey("*"))
            {
                var nextType = Transitions.ContainsKey(message.Text) ? Transitions[message.Text] : Transitions["*"];
                if (nextType != null)
                {
                    var nextState = (StateDecorator)Activator.CreateInstance(nextType)!;
                    try
                    {
                        var response = await nextState.ExecuteAsync(message, session);
                        if (!string.IsNullOrEmpty(response))
                            session.SetLastResponse(response);
                        return nextState.IsLast ? null : nextState;
                    }
                    catch
                    {
                        return this;
                    }
                }
                return null;
            }
            
            return null;
        }
    }
}
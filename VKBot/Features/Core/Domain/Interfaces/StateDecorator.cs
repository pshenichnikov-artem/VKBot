using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;
using Serilog;

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
            {
                Log.Information("[{ClassName}] Пользователь {UserId} отменил действие из {State}", GetType().Name, message.UserId, GetType().Name);
                return new BaseState();
            }
                
            if (Transitions.ContainsKey(message.Text) || Transitions.ContainsKey("*"))
            {
                var nextType = Transitions.ContainsKey(message.Text) ? Transitions[message.Text] : Transitions["*"];
                if (nextType != null)
                {
                    Log.Information("[{ClassName}] Пользователь {UserId} выполняет команду {Command} в состоянии {State}", 
                        GetType().Name, message.UserId, message.Text, GetType().Name);
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
            
            Log.Information("[{ClassName}] Пользователь {UserId} отправил нераспознанное сообщение {Message} в состоянии {State}", 
                GetType().Name, message.UserId, message.Text, GetType().Name);
            return null;
        }
    }
}
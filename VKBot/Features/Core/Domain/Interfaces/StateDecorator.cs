using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;
using VKBot.Features.Core.Domain.Enums;
using Serilog;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Domain.Interfaces
{
    public abstract class StateDecorator
    {
        protected abstract Dictionary<(string command, UserRole role), Type> Transitions { get; }
        protected virtual bool IsLast => false;

        protected abstract Task<StateResult> ExecuteAsync(UserMessage message, UserSession session);

        public async Task<(StateDecorator?, StateResult)> ProcessAsync(UserMessage message, UserSession session)
        {
            if (IsCancelCommand(message.Text))
                return HandleCancel(message.UserId);
                
            var nextType = FindTransition(message.Text, message.UserId);
            
            if (nextType != null)
                return await ExecuteTransition(nextType, message, session);
                
            return HandleUnknownCommand(message.UserId, message.Text);
        }
        
        private bool IsCancelCommand(string text) => text == "/cancel";
        
        private (StateDecorator?, StateResult) HandleCancel(long userId)
        {
            Log.Information("[{ClassName}] Пользователь {UserId} отменил действие", GetType().Name, userId);
            return (new BaseState(), StateResult.Success());
        }
        
        private Type? FindTransition(string command, long userId)
        {
            var userRole = GetUserRole(userId);
            
            if (Transitions.ContainsKey((command, userRole)))
                return Transitions[(command, userRole)];
                
            if (Transitions.ContainsKey(("*", userRole)))
                return Transitions[("*", userRole)];
                
            return null;
        }
        
        private async Task<(StateDecorator?, StateResult)> ExecuteTransition(Type nextType, UserMessage message, UserSession session)
        {
            var nextState = (StateDecorator)Activator.CreateInstance(nextType)!;
            
            Log.Information("[{ClassName}] Пользователь {UserId} выполняет команду {Command}", 
                GetType().Name, message.UserId, message.Text);
                
            try
            {
                var result = await nextState.ExecuteAsync(message, session);
                if (!result.IsSuccess)
                    return (this, result);
                    
                return (nextState.IsLast ? null : nextState, result);
            }
            catch
            {
                return (this, StateResult.Failure("Ошибка выполнения команды"));
            }
        }
        
        private (StateDecorator?, StateResult) HandleUnknownCommand(long userId, string text)
        {
            Log.Information("[{ClassName}] Пользователь {UserId} отправил нераспознанное сообщение {Message}", 
                GetType().Name, userId, text);
            return (null, StateResult.Failure("Неизвестная команда."));
        }
        
        private UserRole GetUserRole(long userId)
        {
            // TODO: реализовать проверку роли через сервис
            return userId == 651565729 ? UserRole.Admin : UserRole.Student;
        }
    }
}
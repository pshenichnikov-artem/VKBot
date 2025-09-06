using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Commands;
using VKBot.Features.Core.Domain.Enums;
using Serilog;
using VKBot.Features.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Models;

namespace VKBot.Features.Core.Domain.Interfaces
{
    public abstract class StateDecorator
    {
        public static IServiceProvider ServiceProvider { get; set; } = null!;
        protected abstract Dictionary<(string command, UserRole? role), Type> Transitions { get; }
        protected virtual bool IsLast => false;

        protected abstract Task<StateResult> ExecuteAsync(UserMessage message, UserSession session);
        
        protected virtual async Task<(StateDecorator?, StateResult)?> ValidateTransitionAsync(UserMessage message, UserSession session)
        {
            return null;
        }

        public async Task<(StateDecorator?, StateResult)> ProcessAsync(UserMessage message, UserSession session)
        {
            if (IsCancelCommand(message.Text))
                return HandleCancel(message.UserId);
                
            if (message.Text == "/debug-get-state")
                return HandleDebugGetState(session);
                
            var validationResult = await ValidateTransitionAsync(message, session);
            if (validationResult.HasValue)
                return validationResult.Value;
                
            var nextType = FindTransition(message.Text, message.UserId);
            
            if (nextType != null)
                return await ExecuteTransition(nextType, message, session);
                
            return HandleUnknownCommand(message.UserId, message.Text);
        }
        
        protected bool IsCancelCommand(string text) => text == "/cancel";

        protected (StateDecorator?, StateResult) HandleCancel(long userId)
        {
            Log.Information("[{ClassName}] Пользователь {UserId} отменил действие", GetType().Name, userId);
            return ((StateDecorator)ServiceProvider.GetService(typeof(BaseState))!, StateResult.Success());
        }
        
        protected (StateDecorator?, StateResult) HandleDebugGetState(UserSession session)
        {
            var stateInfo = $"Текущее состояние: {GetType().Name}\nДанные сессии: {string.Join(", ", session.Data.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
            return (this, StateResult.Success(stateInfo));
        }

        protected Type? FindTransition(string command, long userId)
        {
            var userRole = GetUserRole(userId);
            
            // Поиск по команде и роли
            if (Transitions.ContainsKey((command, userRole)))
                return Transitions[(command, userRole)];
                
            // Поиск по команде без роли
            if (Transitions.ContainsKey((command, null)))
                return Transitions[(command, null)];
                
            // Поиск по универсальной команде с ролью
            if (Transitions.ContainsKey(("*", userRole)))
                return Transitions[("*", userRole)];
                
            // Поиск по универсальной команде без роли
            if (Transitions.ContainsKey(("*", null)))
                return Transitions[("*", null)];
                
            return null;
        }

        protected async Task<(StateDecorator?, StateResult)> ExecuteTransition(Type nextType, UserMessage message, UserSession session)
        {
            var nextState = (StateDecorator)ServiceProvider.GetService(nextType)!;
            
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

        protected (StateDecorator?, StateResult) HandleUnknownCommand(long userId, string text)
        {
            Log.Information("[{ClassName}] Пользователь {UserId} отправил нераспознанное сообщение {Message}", 
                GetType().Name, userId, text);
            return (null, StateResult.Failure("Неизвестная команда."));
        }

        protected UserRole GetUserRole(long userId)
        {
            // TODO: реализовать проверку роли через сервис
            return userId == 651565729 ? UserRole.Admin : UserRole.Student;
        }
        
        protected VkKeyboard? CreateKeyboardFromCommands(long userId)
        {
            var userRole = GetUserRole(userId);
            var commands = Transitions.Keys
                .Where(k => k.command != "*" && k.command[0] != '/' && (k.role == null || k.role == userRole))
                .Select(k => k.command)
                .Distinct()
                .ToList();
                
            if (commands.Count == 0) return null;
            
            var buttons = commands.Select(cmd => new List<VkButton>
            {
                new()
                {
                    Action = new VkButtonAction { Label = cmd },
                    Color = "secondary"
                }
            }).ToList();
            
            return new VkKeyboard
            {
                Inline = true,
                Buttons = buttons
            };
        }
    }
}
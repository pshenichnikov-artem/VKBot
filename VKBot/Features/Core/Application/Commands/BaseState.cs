using Serilog;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands
{
    public class BaseState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole role), Type> Transitions => new()
        {
            { ("/start", UserRole.Student), typeof(User.StartStateUser) },
            { ("/help", UserRole.Student), typeof(User.HelpStateUser) },
            { ("/start", UserRole.Admin), typeof(StartState) },
            { ("/help", UserRole.Admin), typeof(HelpState) },
            { ("/send", UserRole.Admin), typeof(SendGroupState) },
            { ("/alarm", UserRole.Admin), typeof(AlarmState) },
            { ("/excel", UserRole.Admin), typeof(ExcelState) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Неизвестная команда. Используйте /start для начала работы."));
        }
    }
}
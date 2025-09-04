using Serilog;
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Application.Commands.User;
using VKBot.Features.Core.Application.Commands.User.Registration;
using VKBot.Features.Core.Application.Commands.Admin.Confirmation;
using VKBot.Features.Core.Application.Commands.Admin.Groups;
using VKBot.Features.Core.Application.Commands.Admin.Students;

namespace VKBot.Features.Core.Application.Commands
{
    public class BaseState : StateDecorator
    {
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("/start", UserRole.Student), typeof(StartStateUser) },
            { ("/help", UserRole.Student), typeof(HelpStateUser) },
            { ("/register", UserRole.Student), typeof(RegisterState) },

            { ("/register", null), typeof(RegisterState) },
            { ("/start", null), typeof(StartStateUser) },
            { ("/help", null), typeof(HelpStateUser) },

            { ("/start", UserRole.Admin), typeof(StartState) },
            { ("/help", UserRole.Admin), typeof(HelpState) },
            { ("/send", UserRole.Admin), typeof(SendGroupState) },
            { ("/alarm", UserRole.Admin), typeof(AlarmState) },
            { ("/excel", UserRole.Admin), typeof(ExcelState) },
            { ("/students", UserRole.Admin), typeof(StudentsListState) },
            { ("/questions", UserRole.Admin), typeof(UnansweredStudentsState) },
            { ("/confirm", UserRole.Admin), typeof(PendingMessagesState) },
            { ("/addGroup", UserRole.Admin), typeof(EnterGroupNameState) },
            { ("/deleteGroup", UserRole.Admin), typeof(EnterDeleteGroupNameState) }
        }; 

            { ("/test", UserRole.Student), typeof(TestState) },
            { ("/test", UserRole.Admin), typeof(TestStateAdmin) }
        };
        
        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Неизвестная команда. Используйте /start для начала работы."));
        }
    }
}
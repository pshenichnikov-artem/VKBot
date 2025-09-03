using VKBot.Features.Core.Domain.Interfaces;

namespace VKBot.Features.Core.Application.Commands
{
    public class BaseState : StateDecorator
    {
        protected override Dictionary<string, Type> Transitions => new()
        {
            { "/send", typeof(SendGroupState) },
            { "/start", typeof(StartState) },
            { "/help", typeof(HelpState) },
            { "/alarm", typeof(AlarmState) },
            { "/excel", typeof(ExcelState) }
        };
    }
}
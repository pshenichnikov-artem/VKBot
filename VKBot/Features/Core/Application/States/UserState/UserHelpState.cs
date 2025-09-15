using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using System.Reflection;

namespace VKBot.Features.Core.Application.States.UserState;

[State("Справка", UserRole.Student)]
[Description(0, "📚 Справка по командам")]
public class UserHelpState : BaseState
{
    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        var help = "📚 Доступные команды:\n\n";
        
        var stateTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .Select(t => new { Type = t, StateAttr = t.GetCustomAttribute<StateAttribute>() })
            .Where(x => x.StateAttr != null && x.StateAttr.IsEntryState == true && x.StateAttr.AllowedRoles != null && x.StateAttr.AllowedRoles.Contains(UserRole.Student))
            .Where(x => x.StateAttr!.PayloadType == null)
            .ToList();

        foreach (var state in stateTypes)
        {
            var descAttrs = state.Type.GetCustomAttributes<DescriptionAttribute>();
            var firstDesc = descAttrs.FirstOrDefault(d => d.Step == 0);
            var description = firstDesc?.Text ?? "Описание отсутствует";
            help += $"{state.StateAttr!.Command} -- {description}\n";
        }

        help += "\nℹ️ Общие команды:\n/Отмена -- Отменить любую текущую команду";
        
        return new StateResult(help.TrimEnd(), StateAction.End);
    }
}

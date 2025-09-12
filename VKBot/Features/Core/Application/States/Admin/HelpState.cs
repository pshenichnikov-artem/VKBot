using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using System.Reflection;

namespace VKBot.Features.Core.Application.States;

[State("Помощь", UserRole.Admin)]
[Description(0, "📚 Справка по командам")]
public class HelpState : BaseState
{
    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        var help = "📚 Доступные команды администратора:\n\n";
        
        var stateTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .Select(t => new { Type = t, StateAttr = t.GetCustomAttribute<StateAttribute>() })
            .Where(x => x.StateAttr != null && x.StateAttr.AllowedRoles != null && x.StateAttr.AllowedRoles.Contains(UserRole.Admin))
            .ToList();

        foreach (var state in stateTypes)
        {
            var descAttrs = state.Type.GetCustomAttributes<DescriptionAttribute>();
            var firstDesc = descAttrs.FirstOrDefault(d => d.Step == 0);
            var description = firstDesc?.Text ?? "Описание отсутствует";
            help += $"{state.StateAttr!.Command} -- {description}\n";
        }

        help += "\nℹ️ Общие команды:\n/cancel -- Отменить любую текущую команду";
        
        return new StateResult(help.TrimEnd(), StateAction.End);
    }
}

using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace VKBot.Features.Core.Application.States.UserState;

public class UserHelpState : BaseState
{
    public UserHelpState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => "Справка по командам";
    public override bool IsEntryPoint => true;
    public override string? Command => "/help";
    public override UserRole[] AllowedRoles => new[] { UserRole.Student };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        var stateDiscoveryService = _serviceProvider.GetService<IStateDiscoveryService>();
        if (stateDiscoveryService == null)
        {
            return StateResult.Success("Сервис недоступен", StateAction.End);
        }

        var help = "Доступные команды:\n";
        var states = stateDiscoveryService.FindStates(isEntryPoint: true, userRole: UserRole.Student);
        
        foreach (var state in states)
        {
            if (!string.IsNullOrEmpty(state.Command))
            {
                help += $"{state.Command} -- {state.Description}\n";
            }
        }

        return StateResult.Success(help.TrimEnd(), StateAction.End);
    }
}
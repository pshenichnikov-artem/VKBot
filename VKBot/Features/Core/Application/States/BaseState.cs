using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace VKBot.Features.Core.Application.States;

public abstract class BaseState
{
    protected IServiceProvider _serviceProvider { get; private set; }
    protected int _step = 0;

    protected BaseState(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public abstract string Description { get; }
    public abstract bool IsEntryPoint { get; }
    public abstract string? Command { get; }
    public abstract UserRole[] AllowedRoles { get; }
    
    protected abstract Dictionary<int, Type[]> AvailableStates { get; }

    public StateResult? CheckTransition(UserMessage message)
    {
        if (!AvailableStates.TryGetValue(_step, out var availableStates))
            return null;

        foreach (var stateType in availableStates)
        {
            var state = (BaseState)_serviceProvider.GetRequiredService(stateType);
            if (state.Command == message.Text?.ToLower().Trim())
            {
                return StateResult.Success("", StateAction.Next, stateType);
            }
        }

        return null;
    }

    public abstract Task<StateResult> ExecuteAsync(UserMessage message);
    
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
}
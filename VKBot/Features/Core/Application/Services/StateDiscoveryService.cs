using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Services;

public class StateDiscoveryService : IStateDiscoveryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, StateInfo> _stateInfoCache = new();
    private readonly List<StateInfo> _allStates = new();

    public StateDiscoveryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeStates();
    }

    private void InitializeStates()
    {
        var assembly = typeof(BaseState).Assembly;
        var stateTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .ToArray();

        foreach (var stateType in stateTypes)
        {
            var state = (BaseState)_serviceProvider.GetRequiredService(stateType);
            
            var stateInfo = new StateInfo
            {
                Type = stateType,
                Command = state.Command,
                Description = state.Description,
                IsEntryPoint = state.IsEntryPoint,
                AllowedRoles = state.AllowedRoles
            };
            
            _stateInfoCache[stateType] = stateInfo;
            _allStates.Add(stateInfo);
        }
    }

    public IEnumerable<StateInfo> FindStates(bool? isEntryPoint = null, string? command = null, UserRole? userRole = null)
    {
        var query = _allStates.AsEnumerable();

        if (isEntryPoint.HasValue)
            query = query.Where(s => s.IsEntryPoint == isEntryPoint.Value);

        if (!string.IsNullOrEmpty(command))
            query = query.Where(s => s.Command?.Equals(command, StringComparison.OrdinalIgnoreCase) == true);

        if (userRole.HasValue)
            query = query.Where(s => s.AllowedRoles.Contains(userRole.Value));

        return query;
    }

    public StateInfo? GetStateInfo(Type stateType)
    {
        return _stateInfoCache.TryGetValue(stateType, out var info) ? info : null;
    }
}
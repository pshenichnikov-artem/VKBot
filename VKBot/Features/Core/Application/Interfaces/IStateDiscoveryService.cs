using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Interfaces;

public interface IStateDiscoveryService
{
    IEnumerable<StateInfo> FindStates(bool? isEntryPoint = null, string? command = null, UserRole? userRole = null);
    StateInfo? GetStateInfo(Type stateType);
}

public class StateInfo
{
    public Type Type { get; set; } = null!;
    public string? Command { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEntryPoint { get; set; }
    public UserRole[] AllowedRoles { get; set; } = Array.Empty<UserRole>();
}
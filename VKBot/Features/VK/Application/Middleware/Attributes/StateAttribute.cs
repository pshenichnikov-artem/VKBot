using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.VK.Application.Middleware.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StateAttribute : Attribute
{
    public string? Command { get; }
    public PayloadType? PayloadType { get; }
    public bool IsEntryState { get; }
    public UserRole[]? AllowedRoles { get; }

    // Конструктор для не входных состояний с командой
    public StateAttribute(string command)
    {
        Command = command;
        PayloadType = null;
        IsEntryState = false;
        AllowedRoles = null;
    }

    // Конструктор для входных состояний с командой и ролями
    public StateAttribute(string command, params UserRole[] requiredRoles)
    {
        Command = command;
        PayloadType = null;
        IsEntryState = true;
        AllowedRoles = requiredRoles;
    }

    // Конструктор для payload состояний с ролями
    public StateAttribute(PayloadType payloadType, params UserRole[] requiredRoles)
    {
        Command = null;
        PayloadType = payloadType;
        IsEntryState = true;
        AllowedRoles = requiredRoles;
    }
}
using VKBot.Features.Core.Application.Services;

namespace VKBot.Features.Core.Application.Interfaces;

public interface IStateMachineFactory
{
    StateMachine GetOrCreate(long userId);
}
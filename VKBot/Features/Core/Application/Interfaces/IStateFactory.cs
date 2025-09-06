using VKBot.Features.Core.Application.States;

namespace VKBot.Features.Core.Application.Interfaces;

public interface IStateFactory
{
    T Create<T>() where T : BaseState;
    BaseState Create(Type stateType);
}
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.States;

namespace VKBot.Features.Core.Application.Services.Factory;

public class StateFactory : IStateFactory
{
    private readonly IServiceProvider _serviceProvider;

    public StateFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Create<T>() where T : BaseState
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public BaseState Create(Type stateType)
    {
        return (BaseState)_serviceProvider.GetRequiredService(stateType);
    }
}
using Microsoft.Extensions.Caching.Memory;
using VKBot.Features.Core.Application.Interfaces;

namespace VKBot.Features.Core.Application.Services.Factory;

public class StateMachineFactory : IStateMachineFactory
{
    private readonly IMemoryCache _memoryCache;
    private readonly IStateFactory _stateFactory;
    private readonly IServiceProvider _serviceProvider;

    public StateMachineFactory(IMemoryCache memoryCache, IStateFactory stateFactory, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _stateFactory = stateFactory;
        _serviceProvider = serviceProvider;
    }

    public StateMachine GetOrCreate(long userId)
    {
        return StateMachine.GetOrCreate(userId, _memoryCache, _stateFactory, _serviceProvider);
    }
}
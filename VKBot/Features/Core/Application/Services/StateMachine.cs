using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.Services;

[Serializable]
public class StateMachine
{
    [NonSerialized]
    private IMemoryCache _memoryCache;
    [NonSerialized]
    private IServiceProvider _serviceProvider;
    [NonSerialized]
    private ILogger<StateMachine> _logger;
    private readonly long _userId;
    private BaseState? _currentStateInstance;
    private string _cacheKey => $"state_machine_{_userId}";

    public StateMachine(long userId, IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        _userId = userId;
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<StateMachine>>();
    }

    public static StateMachine GetOrCreate(long userId, IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        var cacheKey = $"state_machine_{userId}";

        if (memoryCache.TryGetValue(cacheKey, out StateMachine? cachedMachine))
        {
            cachedMachine!.SetDependencies(memoryCache, serviceProvider);
            return cachedMachine;
        }

        return new StateMachine(userId, memoryCache, serviceProvider);
    }

    public void SetDependencies(IMemoryCache memoryCache, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<StateMachine>>();

        _logger?.LogInformation("[StateMachine] Восстановлено из кэша: {StateType}", _currentStateInstance?.GetType().Name ?? "None");
        
        if (_currentStateInstance != null)
        {
            _currentStateInstance.ReInject(_serviceProvider);
        }
    }

    public async Task<StateResult> ProcessMessage(UserMessage message, BaseState? newState = null)
    {
        if (message.Text.Equals("Отмена", StringComparison.InvariantCultureIgnoreCase))
        {
            _currentStateInstance = null;
            _memoryCache.Remove(_cacheKey);
            return new StateResult("Операция отменена", StateAction.End);
        }

        if (message.Text.Equals("Помощь", StringComparison.InvariantCultureIgnoreCase) && _currentStateInstance != null)
        {
            var type = _currentStateInstance.GetType();
            var description = type
                .GetCustomAttributes<DescriptionAttribute>()
                .Where(a => a.Step == _currentStateInstance.Step)
                .Select(a => a.Text)
                .SingleOrDefault() ?? "Упс... тут нет подсказки";

            return new StateResult(description, StateAction.Stay);
        }

        if (newState != null)
        {
            return await StartState(newState.GetType(), message);
        }

        if (_currentStateInstance == null)
        {
            _logger.LogInformation("[StateMachine] Нет текущего состояния");
            return new StateResult("Ошибка сервера", StateAction.End);
        }

        var commandTransition = CheckCommandTransition(message);
        if (commandTransition != null)
        {
            _logger?.LogInformation("[StateMachine] Переход по команде: {CurrentState} -> {NextState}", _currentStateInstance.GetType().Name, commandTransition.GetType().Name);
            return await StartState(commandTransition.GetType(), message);
        }

        // Затем обычная обработка
        var result = await _currentStateInstance.ExecuteAsync(message);
        if (result.Action != StateAction.Stay)
        {
            _logger?.LogInformation("[StateMachine] {StateType} выполнен: Action={Action}", _currentStateInstance.GetType().Name, result.Action);
        }
        return await ProcessStateResult(result, message);
    }

    private async Task<StateResult> ProcessStateResult(StateResult result, UserMessage originalMessage)
    {
        switch (result.Action)
        {
            case StateAction.End:
                _logger?.LogInformation("[StateMachine] Завершено: {StateType}", _currentStateInstance?.GetType().Name ?? "None");
                _currentStateInstance = null;
                _memoryCache.Remove(_cacheKey);

                //TODO
                    break;
            case StateAction.Next:
                if (result.NextStateType != null)
                {
                    _currentStateInstance = (BaseState)_serviceProvider.GetRequiredService(result.NextStateType);
                    var nextResult = await _currentStateInstance.ExecuteAsync(originalMessage);
                    SaveState();
                    return nextResult;
                }
                break;
            case StateAction.Stay:
                SaveState();
                break;
        }

        return result;
    }

    private async Task<StateResult> StartState(Type stateType, UserMessage message)
    {
        _currentStateInstance = (BaseState)_serviceProvider.GetRequiredService(stateType);
        var result = await _currentStateInstance.ExecuteAsync(message);

        return await ProcessStateResult(result, message);
    }

    private void SaveState()
    {
        _memoryCache.Set(_cacheKey, this, TimeSpan.FromMinutes(5));
    }

    private BaseState? CheckCommandTransition(UserMessage message)
    {
        if (!string.IsNullOrEmpty(message.Text))
        {
            return FindStateByCommand(message.Text.ToLower().Trim());
        }

        return null;
    }

    private BaseState? FindStateByCommand(string command)
    {
        var stateType = System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .FirstOrDefault(t => t.GetCustomAttribute<StateAttribute>()?.Command == command);
        
        return stateType != null ? (BaseState)_serviceProvider.GetRequiredService(stateType) : null;
    }


}


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.Services;

[Serializable]
public class StateMachine
{
    [NonSerialized]
    private IMemoryCache _memoryCache;
    [NonSerialized]
    private IStateFactory _stateFactory;
    [NonSerialized]
    private IServiceProvider _serviceProvider;
    [NonSerialized]
    private ILogger<StateMachine>? _logger;
    [NonSerialized]
    private IStateDiscoveryService? _stateDiscoveryService;
    private readonly long _userId;
    private BaseState? _currentStateInstance;
    private string _cacheKey => $"state_machine_{_userId}";

    public StateMachine(long userId, IMemoryCache memoryCache, IStateFactory stateFactory, IServiceProvider serviceProvider)
    {
        _userId = userId;
        _memoryCache = memoryCache;
        _stateFactory = stateFactory;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILogger<StateMachine>>();
        _stateDiscoveryService = serviceProvider.GetService<IStateDiscoveryService>();
    }

    public static StateMachine GetOrCreate(long userId, IMemoryCache memoryCache, IStateFactory stateFactory, IServiceProvider serviceProvider)
    {
        var cacheKey = $"state_machine_{userId}";

        if (memoryCache.TryGetValue(cacheKey, out StateMachine? cachedMachine))
        {
            cachedMachine!.SetDependencies(memoryCache, stateFactory, serviceProvider);
            return cachedMachine;
        }

        return new StateMachine(userId, memoryCache, stateFactory, serviceProvider);
    }

    public void SetDependencies(IMemoryCache memoryCache, IStateFactory stateFactory, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _stateFactory = stateFactory;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILogger<StateMachine>>();
        _stateDiscoveryService = serviceProvider.GetService<IStateDiscoveryService>();

        _logger?.LogInformation("[StateMachine] Восстановлено из кэша: {StateType}", _currentStateInstance?.GetType().Name ?? "None");

        // Обновляем ServiceProvider в текущем состоянии
        _currentStateInstance?.SetServiceProvider(serviceProvider);
    }

    public async Task<StateResult> ProcessMessage(UserMessage message)
    {

        if (message.Text == "/cancel")
        {
            _currentStateInstance = null;
            _memoryCache.Remove(_cacheKey);
            return StateResult.Success("Операция отменена", StateAction.End);
        }

        if (message.Text == "/help" && _currentStateInstance != null)
        {
            return StateResult.Success(_currentStateInstance.Description, StateAction.Stay);
        }

        if (_currentStateInstance == null)
        {
            return await HandleCommand(message);
        }

        // Сначала проверяем переходы
        var transition = _currentStateInstance.CheckTransition(message);
        if (transition != null)
        {
            _logger?.LogInformation("[StateMachine] Переход: {CurrentState} -> {NextState}", _currentStateInstance.GetType().Name, transition.NextStateType?.Name ?? "None");
            return await ProcessStateResult(transition, message);
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
                break;
            case StateAction.Next:
                if (result.NextStateType != null)
                {
                    _currentStateInstance = _stateFactory.Create(result.NextStateType);
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

    private async Task<StateResult> HandleCommand(UserMessage message)
    {
        var userRole = await GetUserRole(message.UserId);
        var command = message.Text?.ToLower();
        
        if (string.IsNullOrEmpty(command) || _stateDiscoveryService == null)
        {
            return StateResult.Success("Неизвестная команда");
        }
        
        var matchingState = _stateDiscoveryService.FindStates(isEntryPoint: true, command: command, userRole: userRole).FirstOrDefault();
        if (matchingState != null)
        {
            _logger?.LogInformation("[StateMachine] Запуск команды: {StateType}", matchingState.Type.Name);
            return await StartState(matchingState.Type, message);
        }
        
        var availableStates = _stateDiscoveryService.FindStates(isEntryPoint: true, userRole: userRole).ToList();
        
        if (availableStates.Any())
        {
            var keyboard = VkKeyboard.Create(oneTime: true);
            var commands = availableStates.Where(s => !string.IsNullOrEmpty(s.Command)).Select(s => s.Command!).ToList();
            
            for (int i = 0; i < commands.Count; i += 2)
            {
                keyboard.AddRow();
                keyboard.AddButton(commands[i], VkButtonColor.Primary);
                if (i + 1 < commands.Count)
                {
                    keyboard.AddButton(commands[i + 1], VkButtonColor.Primary);
                }
            }
            
            return StateResult.Success($"Неизвестная команда. Доступные команды: {string.Join(", ", commands)}", StateAction.End, keyboard: keyboard);
        }
        
        return StateResult.Success("Ожидайте подтверждения от администратора.");
    }
    
    private async Task<UserRole> GetUserRole(long userId)
    {
        if (userId == 651565729 || userId == 562436407) return UserRole.Admin;
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.VkUserId == userId);
        
        // Если пользователь не зарегистрирован или не подтвержден
        if (user == null || !user.IsConfirmed) return UserRole.Unregistered;
        
        if (user.Role == "Admin") return UserRole.Admin;
        return UserRole.Student;
    }

    private async Task<StateResult> StartState(Type stateType, UserMessage message)
    {
        _currentStateInstance = _stateFactory.Create(stateType);
        var result = await _currentStateInstance.ExecuteAsync(message);

        return await ProcessStateResult(result, message);
    }

    private void SaveState()
    {
        _memoryCache.Set(_cacheKey, this, TimeSpan.FromMinutes(5));
    }

    public Type? GetCurrentStateType()
    {
        return _currentStateInstance?.GetType();
    }

    public string GetCurrentStateDescription()
    {
        return _currentStateInstance?.Description ?? "Нет состояния";
    }
}


using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.Core.Application.Services;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class ConfirmUsersState : BaseState
{
    private List<User> _unconfirmedUsers = new();
    private int _currentUserIndex = 0;

    public ConfirmUsersState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "Подтверждение пользователей\nПолучение списка неподтвержденных пользователей, которые зарегистрировались в системе",
        1 => "Подтверждение пользователей\nВыберите действие: 'Подтвердить всех' (массовое подтверждение) или 'Перейти к подтверждению' (пошаговое рассмотрение)",
        2 => "Подтверждение пользователей\nПошаговое рассмотрение каждого пользователя. Выберите: 'Подтвердить' (доступ к системе), 'Заблокировать' (запрет доступа), 'Пропустить' (оставить на потом)",
        _ => "Неизвестный шаг"
    };
    
    public override bool IsEntryPoint => true;
    public override string? Command => "/confirm";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowUnconfirmedUsers(),
            1 => await ProcessMainAction(message),
            2 => await ProcessUserConfirmation(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowUnconfirmedUsers()
    {
        _step = 1;
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        _unconfirmedUsers = await context.Users
            .Include(u => u.Group)
            .Where(u => !u.IsConfirmed && !u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!_unconfirmedUsers.Any())
        {
            return StateResult.Success("Нет неподтвержденных пользователей", StateAction.End);
        }
        
        var usersList = "Неподтвержденные пользователи:\n";
        
        foreach (var user in _unconfirmedUsers)
        {
            usersList += $"{user.Group?.Name ?? "Без группы"} {user.FullName} ID: {user.VkUserId}\n";
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить всех", VkButtonColor.Positive);
        keyboard.AddRow();
        keyboard.AddButton("Перейти к подтверждению", VkButtonColor.Primary);
        
        return StateResult.Success(usersList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessMainAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        if (action == "подтвердить всех")
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var userIds = _unconfirmedUsers.Select(u => u.VkUserId).ToList();
            var usersToConfirm = await context.Users.Where(u => userIds.Contains(u.VkUserId)).ToListAsync();
            
            var notificationService = scope.ServiceProvider.GetRequiredService<UserNotificationService>();
            
            foreach (var user in usersToConfirm)
            {
                user.IsConfirmed = true;
                user.Role = UserRole.Student.ToString();
            }
            
            await context.SaveChangesAsync();
            
            foreach (var user in usersToConfirm)
            {
                await notificationService.SendUserConfirmedNotification(user.VkUserId);
            }
            
            return StateResult.Success($"Количество подтвержденных пользователей: {usersToConfirm.Count}", StateAction.End);
        }
        
        if (action == "перейти к подтверждению")
        {
            _step = 2;
            _currentUserIndex = 0;
            return ShowCurrentUser();
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить всех", VkButtonColor.Positive);
        keyboard.AddRow();
        keyboard.AddButton("Перейти к подтверждению", VkButtonColor.Primary);
        
        return StateResult.Success("Неизвестное действие", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessUserConfirmation(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var currentUser = _unconfirmedUsers[_currentUserIndex];
        var dbUser = await context.Users.FirstAsync(u => u.VkUserId == currentUser.VkUserId);
        var notificationService = scope.ServiceProvider.GetRequiredService<UserNotificationService>();
        
        switch (action)
        {
            case "подтвердить":
                dbUser.IsConfirmed = true;
                dbUser.Role = UserRole.Student.ToString();
                await context.SaveChangesAsync();
                await notificationService.SendUserConfirmedNotification(dbUser.VkUserId);
                break;
            case "заблокировать":
                dbUser.IsBlocked = true;
                await context.SaveChangesAsync();
                await notificationService.SendUserBlockedNotification(dbUser.VkUserId);
                break;
            case "пропустить":
                break;
        }
        
        _currentUserIndex++;
        
        if (_currentUserIndex >= _unconfirmedUsers.Count)
        {
            return StateResult.Success("Все пользователи обработаны", StateAction.End);
        }
        
        return ShowCurrentUser();
    }

    private StateResult ShowCurrentUser()
    {
        var user = _unconfirmedUsers[_currentUserIndex];
        var userInfo = $"Пользователь {_currentUserIndex + 1} из {_unconfirmedUsers.Count}:\n";
        userInfo += $"{user.Group?.Name ?? "Без группы"} {user.FullName} ID: {user.VkUserId}";
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить", VkButtonColor.Positive);
        keyboard.AddButton("Заблокировать", VkButtonColor.Negative);
        keyboard.AddRow();
        keyboard.AddButton("Пропустить", VkButtonColor.Secondary);
        
        return StateResult.Success(userInfo, StateAction.Stay, keyboard: keyboard);
    }
}
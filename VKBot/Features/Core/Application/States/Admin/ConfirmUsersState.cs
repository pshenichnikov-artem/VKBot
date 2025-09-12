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
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("Одобрить", UserRole.Admin)]
[Description(0, "Подтверждение пользователей\nПолучение списка неподтвержденных пользователей, которые зарегистрировались в системе")]
[Description(1, "Подтверждение пользователей\nВыберите действие: 'Подтвердить всех' (массовое подтверждение) или 'Перейти к подтверждению' (пошаговое рассмотрение)")]
[Description(2, "Подтверждение пользователей\nПошаговое рассмотрение каждого пользователя. Выберите: 'Подтвердить' (доступ к системе), 'Заблокировать' (запрет доступа), 'Пропустить' (оставить на потом)")]
public class ConfirmUsersState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly UserNotificationService _notificationService;
    private List<User> _unconfirmedUsers = new();
    private int _currentUserIndex = 0;

    public ConfirmUsersState(AppDbContext context, UserNotificationService notificationService)
    { 
        _context = context;
        _notificationService = notificationService;
    }


    


    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowUnconfirmedUsers(),
            1 => await ProcessMainAction(message),
            2 => await ProcessUserConfirmation(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowUnconfirmedUsers()
    {
        Step = 1;
        
        _unconfirmedUsers = await _context.Users
            .Include(u => u.Group)
            .Where(u => !u.IsConfirmed && !u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!_unconfirmedUsers.Any())
        {
            return new StateResult("Нет неподтвержденных пользователей", StateAction.End);
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
        
        return new StateResult(usersList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessMainAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        if (action == "подтвердить всех")
        {
            var userIds = _unconfirmedUsers.Select(u => u.VkUserId).ToList();
            var usersToConfirm = await _context.Users.Where(u => userIds.Contains(u.VkUserId)).ToListAsync();
            
            foreach (var user in usersToConfirm)
            {
                user.IsConfirmed = true;
                user.Role = UserRole.Student.ToString();
            }
            
            await _context.SaveChangesAsync();
            
            foreach (var user in usersToConfirm)
            {
                await _notificationService.SendUserConfirmedNotification(user.VkUserId);
            }
            
            return new StateResult($"Количество подтвержденных пользователей: {usersToConfirm.Count}", StateAction.End);
        }
        
        if (action == "перейти к подтверждению")
        {
            Step = 2;
            _currentUserIndex = 0;
            return ShowCurrentUser();
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить всех", VkButtonColor.Positive);
        keyboard.AddRow();
        keyboard.AddButton("Перейти к подтверждению", VkButtonColor.Primary);
        
        return new StateResult("Неизвестное действие", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessUserConfirmation(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        var currentUser = _unconfirmedUsers[_currentUserIndex];
        var dbUser = await _context.Users.FirstAsync(u => u.VkUserId == currentUser.VkUserId);
        
        switch (action)
        {
            case "подтвердить":
                dbUser.IsConfirmed = true;
                dbUser.Role = UserRole.Student.ToString();
                await _context.SaveChangesAsync();
                await _notificationService.SendUserConfirmedNotification(dbUser.VkUserId);
                break;
            case "заблокировать":
                dbUser.IsBlocked = true;
                await _context.SaveChangesAsync();
                await _notificationService.SendUserBlockedNotification(dbUser.VkUserId);
                break;
            case "пропустить":
                break;
        }
        
        _currentUserIndex++;
        
        if (_currentUserIndex >= _unconfirmedUsers.Count)
        {
            return new StateResult("Все пользователи обработаны", StateAction.End);
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
        
        return new StateResult(userInfo, StateAction.Stay, keyboard: keyboard);
    }
}

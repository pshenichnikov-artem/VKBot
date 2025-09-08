using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class ConfirmUsersState : BaseState
{
    private List<User> _unconfirmedUsers = new();
    private int _currentUserIndex = 0;

    public ConfirmUsersState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "✅ Подтверждение новых пользователей\nПросмотр заявок на регистрацию",
        1 => "🎯 Выбор способа обработки\nМассовое или индивидуальное подтверждение",
        2 => "👀 Индивидуальное рассмотрение\nПошаговое принятие решений по каждому пользователю",
        _ => "❓ Неизвестный шаг"
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
            _ => StateResult.Success("❌ Ошибка", StateAction.End)
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
            return StateResult.Success("😌 На данный момент нет новых заявок на регистрацию", StateAction.End);
        }
        
        var usersList = $"📝 Новые заявки на регистрацию ({_unconfirmedUsers.Count}):\n\n";
        
        foreach (var user in _unconfirmedUsers)
        {
            usersList += $"🎓 {user.Group?.Name ?? "Без группы"} — {user.FullName}\n";
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("✅ Подтвердить всех", VkButtonColor.Positive);
        keyboard.AddRow();
        keyboard.AddButton("👀 Рассмотреть по одному", VkButtonColor.Primary);
        
        return StateResult.Success(usersList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessMainAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        if (action?.Contains("подтвердить всех") == true)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var userIds = _unconfirmedUsers.Select(u => u.VkUserId).ToList();
            var usersToConfirm = await context.Users.Where(u => userIds.Contains(u.VkUserId)).ToListAsync();
            
            foreach (var user in usersToConfirm)
            {
                user.IsConfirmed = true;
                user.Role = UserRole.Student.ToString();
            }
            
            await context.SaveChangesAsync();
            
            return StateResult.Success($"✅ Все пользователи успешно подтверждены! 🎉\n\n👥 Подтверждено: {usersToConfirm.Count} чел.", StateAction.End);
        }
        
        if (action?.Contains("рассмотреть") == true)
        {
            _step = 2;
            _currentUserIndex = 0;
            return ShowCurrentUser();
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("✅ Подтвердить всех", VkButtonColor.Positive);
        keyboard.AddRow();
        keyboard.AddButton("👀 Рассмотреть по одному", VkButtonColor.Primary);
        
        return StateResult.Success("⚠️ Пожалуйста, используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessUserConfirmation(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var currentUser = _unconfirmedUsers[_currentUserIndex];
        var dbUser = await context.Users.FirstAsync(u => u.VkUserId == currentUser.VkUserId);
        
        switch (action)
        {
            case var s when s.Contains("подтвердить"):
                dbUser.IsConfirmed = true;
                dbUser.Role = UserRole.Student.ToString();
                await context.SaveChangesAsync();
                break;
            case var s when s.Contains("заблокировать"):
                dbUser.IsBlocked = true;
                await context.SaveChangesAsync();
                break;
            case var s when s.Contains("пропустить"):
                break;
        }
        
        _currentUserIndex++;
        
        if (_currentUserIndex >= _unconfirmedUsers.Count)
        {
            return StateResult.Success("✅ Все пользователи обработаны! 🎉", StateAction.End);
        }
        
        return ShowCurrentUser();
    }

    private StateResult ShowCurrentUser()
    {
        var user = _unconfirmedUsers[_currentUserIndex];
        var userInfo = $"👤 Пользователь {_currentUserIndex + 1} из {_unconfirmedUsers.Count}\n\n🎓 Группа: {user.Group?.Name ?? "Не указана"}\n📝 ФИО: {user.FullName}";
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("✅ Подтвердить", VkButtonColor.Positive);
        keyboard.AddButton("❌ Заблокировать", VkButtonColor.Negative);
        keyboard.AddRow();
        keyboard.AddButton("⏭️ Пропустить", VkButtonColor.Secondary);
        
        return StateResult.Success(userInfo, StateAction.Stay, keyboard: keyboard);
    }
}
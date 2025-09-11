using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.Core.Application.Services;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class BlockedStudentsState : BaseState
{
    private long _studentId;

    public BlockedStudentsState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "🚫 Управление заблокированными студентами\nКоманда для просмотра списка заблокированных студентов с возможностью разблокировки.",
        1 => "📋 Выбор действия\nИспользуйте кнопки для выбора действия с заблокированными студентами.",
        2 => "🔢 Ввод VK ID для разблокировки\nУкажите VK ID студента, которого нужно разблокировать.",
        _ => "❌ Ошибка в процессе управления заблокированными студентами"
    };
    
    public override bool IsEntryPoint => true;
    public override string? Command => "/blocked";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowBlockedStudents(),
            1 => ProcessAction(message),
            2 => await ProcessVkId(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowBlockedStudents()
    {
        _step = 1;
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var blockedStudents = await context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Group)
            .Where(u => u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!blockedStudents.Any())
        {
            return StateResult.Success("✅ Нет заблокированных студентов", StateAction.End);
        }
        
        var studentsList = "🚫 Заблокированные студенты:\n";
        
        foreach (var student in blockedStudents)
        {
            var vkLink = $"https://vk.com/id{student.VkUserId}";
            studentsList += $"{student.FullName} VK ID: {student.VkUserId} {vkLink}\n";
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Разблокировать", VkButtonColor.Positive);
        keyboard.AddButton("Отмена", VkButtonColor.Secondary);
        
        return StateResult.Success(studentsList, StateAction.Stay, keyboard: keyboard);
    }
    
    private StateResult ProcessAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        if (action == "разблокировать")
        {
            _step = 2;
            return StateResult.Success("🔢 Введите VK ID студента:", StateAction.Stay);
        }
        
        if (action == "отмена")
        {
            return StateResult.Success("❌ Операция отменена", StateAction.End);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Разблокировать", VkButtonColor.Positive);
        keyboard.AddButton("Отмена", VkButtonColor.Secondary);
        
        return StateResult.Success("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }
    
    private async Task<StateResult> ProcessVkId(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long vkId))
        {
            return StateResult.Success("❌ Неверный формат\n🔢 Введите корректный VK ID:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var student = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == vkId && u.IsBlocked);
        if (student == null)
        {
            return StateResult.Success($"❌ Студент с VK ID {vkId} не найден среди заблокированных", StateAction.Stay);
        }
        
        student.IsBlocked = false;
        await context.SaveChangesAsync();
        
        var notificationService = scope.ServiceProvider.GetRequiredService<UserNotificationService>();
        await notificationService.SendUserUnblockedNotification(student.VkUserId);
        
        return StateResult.Success($"✅ Студент {student.FullName} успешно разблокирован", StateAction.End);
    }
}
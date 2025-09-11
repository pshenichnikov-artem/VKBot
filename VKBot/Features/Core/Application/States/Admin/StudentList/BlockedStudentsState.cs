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
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("бан", UserRole.Admin)]
[Description(0, "🚫 Управление заблокированными студентами")]
[Description(1, "📋 Выбор действия")]
[Description(2, "🔢 Ввод VK ID для разблокировки")]
public class BlockedStudentsState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly UserNotificationService _notificationService;
    private long _studentId;

    public BlockedStudentsState(AppDbContext context, UserNotificationService notificationService)
    { 
        _context = context;
        _notificationService = notificationService;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowBlockedStudents(),
            1 => ProcessAction(message),
            2 => await ProcessVkId(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowBlockedStudents()
    {
        Step = 1;
        
        var blockedStudents = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Group)
            .Where(u => u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!blockedStudents.Any())
        {
            return new StateResult("✅ Нет заблокированных студентов", StateAction.End);
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
        
        return new StateResult(studentsList, StateAction.Stay, keyboard: keyboard);
    }
    
    private StateResult ProcessAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        if (action == "разблокировать")
        {
            Step = 2;
            return new StateResult("🔢 Введите VK ID студента:", StateAction.Stay);
        }
        
        if (action == "отмена")
        {
            return new StateResult("❌ Операция отменена", StateAction.End);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Разблокировать", VkButtonColor.Positive);
        keyboard.AddButton("Отмена", VkButtonColor.Secondary);
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }
    
    private async Task<StateResult> ProcessVkId(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long vkId))
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите корректный VK ID:", StateAction.Stay);
        }

        var student = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == vkId && u.IsBlocked);
        if (student == null)
        {
            return new StateResult($"❌ Студент с VK ID {vkId} не найден среди заблокированных", StateAction.Stay);
        }
        
        student.IsBlocked = false;
        await _context.SaveChangesAsync();
        
        await _notificationService.SendUserUnblockedNotification(student.VkUserId);
        
        return new StateResult($"✅ Студент {student.FullName} успешно разблокирован", StateAction.End);
    }
}

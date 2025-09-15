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

[State("Удалить")]
[Description(0, "🗑️ Удаление студента")]
[Description(1, "🔍 Поиск студента")]
[Description(2, "⚠️ Подтверждение удаления")]
public class DeleteStudentState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly UserNotificationService _notificationService;
    private string? _studentName;
    private long _studentId;

    public DeleteStudentState(AppDbContext context, UserNotificationService notificationService)
    { 
        _context = context;
        _notificationService = notificationService;
    }
    


    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => RequestStudentName(),
            1 => await ProcessStudentName(message),
            2 => await ProcessConfirmation(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private StateResult RequestStudentName()
    {
        Step = 1;
        return new StateResult("🔍 Введите VK ID студента для удаления:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessStudentName(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long vkId))
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите корректный VK ID:", StateAction.Stay);
        }

        var student = await _context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.VkUserId == vkId && u.Role == UserRole.Student.ToString() && u.IsConfirmed && !u.IsBlocked);
            
        if (student == null)
        {
            return new StateResult($"❌ Студент с VK ID {vkId} не найден\n🔍 Проверьте VK ID и попробуйте снова:", StateAction.Stay);
        }
        
        _studentName = $"{student.Group?.Name} {student.FullName}";
        _studentId = vkId;

        Step = 2;
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Да", VkButtonColor.Negative);
        keyboard.AddButton("Нет", VkButtonColor.Secondary);
        
        return new StateResult($"⚠️ ВНИМАНИЕ!\nУдалить студента {_studentName}?\n\nЭто действие нельзя отменить!", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessConfirmation(UserMessage message)
    {
        var response = message.Text?.ToLower().Trim();
        
        if (response == "да")
        {
            var student = await _context.Users.FirstOrDefaultAsync(u => u.VkUserId == _studentId);
            if (student != null)
            {
                student.IsDeleted = true;
                student.Role = UserRole.Unregistered.ToString();
                await _context.SaveChangesAsync();
                
                await _notificationService.SendUserDeletedNotification(student.VkUserId);
            }
            
            return new StateResult($"✅ Студент {_studentName} успешно удален из системы", StateAction.End);
        }
        
        if (response == "нет")
        {
            return new StateResult("❌ Удаление отменено", StateAction.End);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Да", VkButtonColor.Negative);
        keyboard.AddButton("Нет", VkButtonColor.Secondary);
        
        return new StateResult("❌ Используйте кнопки для выбора:", StateAction.Stay, keyboard: keyboard);
    }
}

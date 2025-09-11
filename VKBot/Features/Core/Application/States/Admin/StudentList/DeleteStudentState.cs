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

public class DeleteStudentState : BaseState
{
    private string? _studentName;
    private long _studentId;

    public DeleteStudentState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "🗑️ Удаление студента\nКоманда для полного удаления студента из системы. Операция необратима!",
        1 => "🔍 Поиск студента\nВведите VK ID студента, которого нужно удалить из системы.",
        2 => $"⚠️ Подтверждение удаления\nПодтвердите удаление студента {_studentName}. Это действие нельзя отменить!",
        _ => "❌ Ошибка в процессе удаления студента"
    };
    
    public override bool IsEntryPoint => false;
    public override string? Command => "удалить";

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override UserRole[] AllowedRoles => [UserRole.Admin];

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => RequestStudentName(),
            1 => await ProcessStudentName(message),
            2 => await ProcessConfirmation(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private StateResult RequestStudentName()
    {
        _step = 1;
        return StateResult.Success("🔍 Введите VK ID студента для удаления:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessStudentName(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long vkId))
        {
            return StateResult.Success("❌ Неверный формат\n🔢 Введите корректный VK ID:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var student = await context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.VkUserId == vkId && u.Role == UserRole.Student.ToString() && u.IsConfirmed && !u.IsBlocked);
            
        if (student == null)
        {
            return StateResult.Success($"❌ Студент с VK ID {vkId} не найден\n🔍 Проверьте VK ID и попробуйте снова:", StateAction.Stay);
        }
        
        _studentName = $"{student.Group?.Name} {student.FullName}";
        _studentId = vkId;

        _step = 2;
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Да", VkButtonColor.Negative);
        keyboard.AddButton("Нет", VkButtonColor.Secondary);
        
        return StateResult.Success($"⚠️ ВНИМАНИЕ!\nУдалить студента {_studentName}?\n\nЭто действие нельзя отменить!", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessConfirmation(UserMessage message)
    {
        var response = message.Text?.ToLower().Trim();
        
        if (response == "да")
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var student = await context.Users.FirstOrDefaultAsync(u => u.VkUserId == _studentId);
            if (student != null)
            {
                student.IsDeleted = true;
                await context.SaveChangesAsync();
                
                var notificationService = scope.ServiceProvider.GetRequiredService<UserNotificationService>();
                await notificationService.SendUserDeletedNotification(student.VkUserId);
            }
            
            return StateResult.Success($"✅ Студент {_studentName} успешно удален из системы", StateAction.End);
        }
        
        if (response == "нет")
        {
            return StateResult.Success("❌ Удаление отменено", StateAction.End);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Да", VkButtonColor.Negative);
        keyboard.AddButton("Нет", VkButtonColor.Secondary);
        
        return StateResult.Success("❌ Используйте кнопки для выбора:", StateAction.Stay, keyboard: keyboard);
    }
}
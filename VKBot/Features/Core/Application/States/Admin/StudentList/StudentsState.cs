using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class StudentsState : BaseState
{
    public StudentsState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "👥 Управление списком студентов\nКоманда для просмотра списка всех активных студентов с возможностью редактирования и удаления.",
        1 => "📋 Выбор действия\nИспользуйте кнопки для выбора действия со студентами.",
        _ => "❌ Ошибка в процессе управления студентами"
    };
    
    public override bool IsEntryPoint => true;
    public override string? Command => "/students";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new()
    {
        { 1, new[] { typeof(EditStudentState), typeof(DeleteStudentState) } }
    };

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowStudents(),
            1 => ProcessAction(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowStudents()
    {
        _step = 1;
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var students = await context.Users
            .Include(u => u.Group)
            .Where(u => u.Role == UserRole.Student.ToString() && u.IsConfirmed && !u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!students.Any())
        {
            return StateResult.Success("👥 Список студентов пуст", StateAction.End);
        }
        
        var studentsList = $"👥 Список студентов ({students.Count}):\n";
        
        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            var vkLink = $"https://vk.com/id{student.VkUserId}";
            studentsList += $"{i + 1}. {student.Group?.Name ?? "Без группы"} {student.FullName} VK ID: [{vkLink}|{student.VkUserId}]\n";
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Изменить", VkButtonColor.Primary);
        keyboard.AddButton("Удалить", VkButtonColor.Negative);
        
        return StateResult.Success(studentsList, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessAction(UserMessage message)
    {
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Изменить", VkButtonColor.Primary);
        keyboard.AddButton("Удалить", VkButtonColor.Negative);
        
        return StateResult.Success("❌ Используйте кнопки для выбора действия", StateAction.Stay, keyboard: keyboard);
    }
}
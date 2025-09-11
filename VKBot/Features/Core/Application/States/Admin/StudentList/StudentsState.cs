using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using System.Threading.Tasks;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("студенты", UserRole.Admin)]
[Transition(1, typeof(EditStudentState), typeof(DeleteStudentState))]
[Description(0, "👥 Управление списком студентов")]
[Description(1, "📋 Выбор действия")]
public class StudentsState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;

    public StudentsState(AppDbContext context)
    { 
        _context = context;
    }
    


    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowStudents(),
            1 => ProcessAction(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowStudents()
    {
        Step = 1;
        
        var students = await _context.Users
            .Include(u => u.Group)
            .Where(u => u.Role == UserRole.Student.ToString() && u.IsConfirmed && !u.IsBlocked)
            .OrderBy(u => u.Group!.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();
            
        if (!students.Any())
        {
            return new StateResult("👥 Список студентов пуст", StateAction.End);
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
        
        return new StateResult(studentsList, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessAction(UserMessage message)
    {
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Изменить", VkButtonColor.Primary);
        keyboard.AddButton("Удалить", VkButtonColor.Negative);
        
        return new StateResult("❌ Используйте кнопки для выбора действия", StateAction.Stay, keyboard: keyboard);
    }
}

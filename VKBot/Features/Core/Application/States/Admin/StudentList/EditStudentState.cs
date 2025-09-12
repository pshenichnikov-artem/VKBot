using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("Изменить")]
[Description(0, "✏️ Редактирование данных студента")]
[Description(1, "🔍 Поиск студента")]
[Description(2, "📝 Ввод нового ФИО")]
public class EditStudentState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private string? _studentName;
    private long _studentId;
    private string? _newName;

    public EditStudentState(AppDbContext context)
    { 
        _context = context;
    }
  


    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => RequestStudentName(),
            1 => await ProcessStudentName(message),
            2 => await ProcessNewName(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private StateResult RequestStudentName()
    {
        Step = 1;
        return new StateResult("🔍 Введите VK ID студента:", StateAction.Stay);
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
        
        _studentName = $"{student.FullName}";
        _studentId = vkId;

        Step = 2;
        return new StateResult($"📝 Введите новое ФИО для {_studentName}:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessNewName(UserMessage message)
    {
        _newName = message.Text?.Trim();
        if (string.IsNullOrEmpty(_newName))
        {
            return new StateResult("❌ ФИО обязательно\n📝 Введите новое ФИО:", StateAction.Stay);
        }

        var student = await _context.Users.FirstOrDefaultAsync(u => u.VkUserId == _studentId);
        if (student != null)
        {
            student.FullName = _newName;
            await _context.SaveChangesAsync();
        }

        return new StateResult($"✅ ФИО студента успешно изменено\nСтарое: {_studentName}\nНовое: {_newName}", StateAction.End);
    }
}

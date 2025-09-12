using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("Факультеты")]
[Description(0, "🏛️ Управление факультетами 1")]
[Description(1, "📋 Выбор действия")]
[Description(2, "📝 Ввод названия факультета")]
public class FacultyState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private string? _action;

    public FacultyState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowFaculties(),
            1 => ProcessAction(message),
            2 => await ProcessFacultyName(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowFaculties()
    {
        Step = 1;
        
        var faculties = await _context.Faculties
            .Include(f => f.Groups)
            .OrderBy(f => f.Name)
            .ToListAsync();
            
        var facultiesList = "🏛️ Управление факультетами:\n";
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Добавить", VkButtonColor.Positive);
        
        if (!faculties.Any())
        {
            facultiesList += "Факультеты отсутствуют";
        }
        else
        {
            foreach (var faculty in faculties)
            {
                facultiesList += $"{faculty.Name} (групп: {faculty.Groups.Count})\n";
            }
            keyboard.AddButton("Удалить", VkButtonColor.Negative);
        }
        
        return new StateResult(facultiesList, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessAction(UserMessage message)
    {
        _action = message.Text?.ToLower().Trim();
        
        if (_action == "добавить")
        {
            Step = 2;
            return new StateResult("📝 Введите название факультета:", StateAction.Stay);
        }
        
        if (_action == "удалить")
        {
            Step = 2;
            return new StateResult("📝 Введите название факультета для удаления:", StateAction.Stay);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Добавить", VkButtonColor.Positive);
        
        var hasFaculties = _context.Faculties.Any();
        if (hasFaculties)
        {
            keyboard.AddButton("Удалить", VkButtonColor.Negative);
        }
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessFacultyName(UserMessage message)
    {
        var facultyName = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(facultyName))
        {
            return new StateResult("❌ Название факультета обязательно:", StateAction.Stay);
        }

        var result = _action switch
        {
            "добавить" => await AddFaculty(facultyName),
            "удалить" => await DeleteFaculty(facultyName),
            _ => "Ошибка обработки действия"
        };

        return new StateResult(result, StateAction.End);
    }
    
    private async Task<string> AddFaculty(string facultyName)
    {
        var existingFaculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name == facultyName);
        if (existingFaculty != null)
        {
            return $"❌ Факультет {facultyName} уже существует";
        }
        
        var faculty = new Faculty { Name = facultyName };
        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();
        
        return $"✅ Факультет {facultyName} добавлен";
    }
    
    private async Task<string> DeleteFaculty(string facultyName)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Groups)
            .FirstOrDefaultAsync(f => f.Name == facultyName);
            
        if (faculty == null)
        {
            return $"❌ Факультет {facultyName} не найден";
        }
        
        if (faculty.Groups.Any())
        {
            return $"❌ Нельзя удалить факультет {facultyName} - есть связанные группы";
        }
        
        _context.Faculties.Remove(faculty);
        await _context.SaveChangesAsync();
        
        return $"✅ Факультет {facultyName} удален";
    }
}
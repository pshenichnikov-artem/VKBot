using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using System.Text.RegularExpressions;
using Group = VKBot.Features.Core.Domain.Entities.Group;

namespace VKBot.Features.Core.Application.States;

[State("группы управление")]
[Description(0, "👥 Управление группами")]
[Description(1, "📋 Выбор действия")]
[Description(2, "🏛️ Выбор факультета")]
[Description(3, "📝 Ввод названия группы")]
public class GroupManagementState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private string? _action;
    private int _facultyId;

    public GroupManagementState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowGroupsByFaculties(),
            1 => await ProcessAction(message),
            2 => await ProcessFacultySelection(message),
            3 => await ProcessGroupName(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowGroupsByFaculties()
    {
        Step = 1;
        
        var faculties = await _context.Faculties
            .Include(f => f.Groups)
            .OrderBy(f => f.Name)
            .ToListAsync();
            
        var groupsList = "👥 Управление группами:\n\n";
        
        if (!faculties.Any())
        {
            groupsList += "Факультеты отсутствуют";
        }
        else
        {
            foreach (var faculty in faculties)
            {
                groupsList += $"🏛️ {faculty.Name}:\n";
                if (faculty.Groups.Any())
                {
                    foreach (var group in faculty.Groups.OrderBy(g => g.Name))
                    {
                        groupsList += $"  • {group.Name}\n";
                    }
                }
                else
                {
                    groupsList += "  Групп нет\n";
                }
                groupsList += "\n";
            }
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Добавить группу", VkButtonColor.Positive);
        if (faculties.Any(f => f.Groups.Any()))
        {
            keyboard.AddButton("Удалить группу", VkButtonColor.Negative);
        }
        
        return new StateResult(groupsList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessAction(UserMessage message)
    {
        _action = message.Text?.ToLower().Trim();
        
        if (_action == "добавить группу" || _action == "удалить группу")
        {
            Step = 2;
            return await ShowFacultySelection();
        }
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
    }

    private async Task<StateResult> ShowFacultySelection()
    {
        var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
        
        if (!faculties.Any())
        {
            return new StateResult("❌ Сначала создайте факультеты", StateAction.End);
        }
        
        var facultiesList = "🏛️ Выберите факультет:\n";
        
        var keyboard = VkKeyboard.Create(false, true);
        for (int i = 0; i < faculties.Count; i++)
        {
            var faculty = faculties[i];
            facultiesList += $"• {faculty.Name}\n";
            
            if (i % 2 == 0) keyboard.AddRow();
            keyboard.AddButton(faculty.Name, VkButtonColor.Primary);
        }
        
        return new StateResult(facultiesList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessFacultySelection(UserMessage message)
    {
        var facultyName = message.Text?.Trim();
        var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name.ToLower() == facultyName.ToLower());
        
        if (faculty == null)
        {
            return new StateResult("❌ Используйте кнопки для выбора факультета", StateAction.Stay);
        }

        _facultyId = faculty.Id;
        Step = 3;
        
        var prompt = _action == "добавить группу" 
            ? $"📝 Введите название группы для факультета {faculty.Name} (формат: ИТ/б-22-1-о):"
            : $"📝 Введите название группы для удаления из факультета {faculty.Name}:";
            
        return new StateResult(prompt, StateAction.Stay);
    }

    private async Task<StateResult> ProcessGroupName(UserMessage message)
    {
        var groupName = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(groupName))
        {
            return new StateResult("❌ Название группы обязательно:", StateAction.Stay);
        }

        var result = _action switch
        {
            "добавить группу" => await AddGroup(groupName),
            "удалить группу" => await DeleteGroup(groupName),
            _ => "Ошибка обработки действия"
        };

        return new StateResult(result, StateAction.End);
    }
    
    private async Task<string> AddGroup(string groupName)
    {
        if (!IsValidGroupFormat(groupName))
        {
            return "❌ Неверный формат группы. Используйте формат: ИТ/б-22-1-о";
        }

        var existingGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
        if (existingGroup != null)
        {
            return $"❌ Группа {groupName} уже существует";
        }
        
        var (cohort, groupNumber) = ParseGroupName(groupName);
        
        var group = new Group 
        { 
            Name = groupName,
            Cohort = cohort,
            GroupNumber = groupNumber,
            FacultyId = _facultyId
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        
        return $"✅ Группа {groupName} добавлена";
    }
    
    private async Task<string> DeleteGroup(string groupName)
    {
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == groupName && g.FacultyId == _facultyId);
        if (group == null)
        {
            return $"❌ Группа {groupName} не найдена в выбранном факультете";
        }
        
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        
        return $"✅ Группа {groupName} удалена";
    }

    private bool IsValidGroupFormat(string groupName)
    {
        var pattern = @"^[А-Я]{2,3}/[а-я]-\d{2}-\d+-[а-я]$";
        return Regex.IsMatch(groupName, pattern);
    }
    
    private (string cohort, short groupNumber) ParseGroupName(string groupName)
    {
        var parts = groupName.Split('-');
        var cohort = string.Join("-", parts.Take(parts.Length - 2));
        var groupNumber = short.Parse(parts[^2]);
        
        return (cohort, groupNumber);
    }
}
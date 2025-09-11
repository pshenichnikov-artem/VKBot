
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Group = VKBot.Features.Core.Domain.Entities.Group;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using System.Threading.Tasks;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("группы", UserRole.Admin)]
[Transition(1, typeof(GroupManagementState), typeof(FacultyState))]
[Description(0, "🏛️ Управление группами и факультетами")]
[Description(1, "📋 Выбор раздела управления")]
public class GroupState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;

    public GroupState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowMainMenu(),
            1 => ProcessMenuSelection(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowMainMenu()
    {
        Step = 1;
        
        var faculties = await _context.Faculties
            .Include(f => f.Groups)
            .OrderBy(f => f.Name)
            .ToListAsync();
            
        var summary = "🏛️ Управление группами и факультетами\n\n";
        summary += $"📊 Статистика:\n";
        summary += $"• Факультетов: {faculties.Count}\n";
        summary += $"• Групп: {faculties.Sum(f => f.Groups.Count)}\n\n";
        
        if (faculties.Any())
        {
            summary += "📋 Факультеты:\n";
            foreach (var faculty in faculties)
            {
                summary += $"• {faculty.Name} ({faculty.Groups.Count} групп)\n";
            }
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Управление группами", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("Управление факультетами", VkButtonColor.Secondary);
        
        return new StateResult(summary, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessMenuSelection(UserMessage message)
    {
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Управление группами", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("Управление факультетами", VkButtonColor.Secondary);
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }
}

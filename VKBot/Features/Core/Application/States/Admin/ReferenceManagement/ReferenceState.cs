using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("Справки", UserRole.Admin)]
[Transition(1, typeof(AddReferenceState), typeof(EditReferenceState), typeof(DeleteReferenceState))]
[Description(0, "📚 Управление справками")]
[Description(1, "📋 Выбор действия")]
public class ReferenceState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;

    public ReferenceState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowReferences(),
            1 => await ProcessAction(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowReferences()
    {
        Step = 1;
        
        var references = await _context.References
            .OrderBy(r => r.Title)
            .ToListAsync();
            
        var referencesList = "📚 Управление справками:\n\n";
        
        if (!references.Any())
        {
            referencesList += "Справки отсутствуют";
        }
        else
        {
            foreach (var reference in references)
            {
                referencesList += $"• {reference.Title}\n";
            }
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton(GetCommand<AddReferenceState>(), VkButtonColor.Positive);

        if (references.Any())
        {
            keyboard.AddRow();
            keyboard.AddButton(GetCommand<EditReferenceState>(), VkButtonColor.Primary);
            keyboard.AddButton(GetCommand<DeleteReferenceState>(), VkButtonColor.Negative);
        }

        return new StateResult(referencesList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessAction(UserMessage message)
    {
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton(GetCommand<AddReferenceState>(), VkButtonColor.Positive);
        
        var hasReferences = await _context.References.AnyAsync();
        if (hasReferences)
        {
            keyboard.AddRow();
            keyboard.AddButton(GetCommand<EditReferenceState>(), VkButtonColor.Primary);
            keyboard.AddButton(GetCommand<DeleteReferenceState>(), VkButtonColor.Negative);
        }
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }
}
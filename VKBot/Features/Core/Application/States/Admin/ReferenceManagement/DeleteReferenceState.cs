using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Application.Utils;
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Application.States;

[State("Удалить справку")]
[Description(0, "📋 Выбор справки для удаления")]
public class DeleteReferenceState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private int _currentPage = 0;

    public DeleteReferenceState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowReferenceSelection(),
            1 => await ProcessReferenceSelection(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowReferenceSelection()
    {
        Step = 1;
        
        var references = await _context.References.OrderBy(r => r.Title).ToListAsync();
        
        var keyboard = KeyboardPagination.CreatePaginatedKeyboard(
            references, 
            _currentPage, 
            r => r.Title, 
            out var pageInfo,
            VkButtonColor.Negative);
        
        return new StateResult($"📋 Выберите справку для удаления{pageInfo}:", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessReferenceSelection(UserMessage message)
    {
        var input = message.Text?.Trim();
        
        if (KeyboardPagination.IsNavigationCommand(input, out var direction))
        {
            var totalCount = await _context.References.CountAsync();
            var newPage = KeyboardPagination.GetValidPage(_currentPage, direction, totalCount);
            if (newPage != _currentPage)
            {
                _currentPage = newPage;
                return await ShowReferenceSelection();
            }
        }
        
        var reference = await _context.References.FirstOrDefaultAsync(r => r.Title == input);
        
        if (reference == null)
        {
            return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
        }

        _context.References.Remove(reference);
        await _context.SaveChangesAsync();
        
        return new StateResult($"✅ Справка \"{reference.Title}\" удалена", StateAction.End);
    }
}
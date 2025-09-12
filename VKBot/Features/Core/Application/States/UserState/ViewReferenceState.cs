using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Application.Utils;

namespace VKBot.Features.Core.Application.States.UserState;

[State("Справка", UserRole.Student)]
[Description(0, "📚 Просмотр справок")]
[Description(1, "📋 Выбор справки")]
public class ViewReferenceState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private int _currentPage = 0;

    public ViewReferenceState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowReferenceList(),
            1 => await ShowReferenceContent(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowReferenceList()
    {
        Step = 1;
        
        var references = await _context.References
            .OrderBy(r => r.Title)
            .ToListAsync();
            
        if (!references.Any())
        {
            return new StateResult("📚 Справки отсутствуют", StateAction.End);
        }
        
        var keyboard = KeyboardPagination.CreatePaginatedKeyboard(
            references, 
            _currentPage, 
            r => r.Title, 
            VkButtonColor.Primary);
        
        return new StateResult("📚 Выберите справку:", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ShowReferenceContent(UserMessage message)
    {
        var input = message.Text?.Trim();
        
        if (KeyboardPagination.IsNavigationCommand(input, out var direction))
        {
            var totalCount = await _context.References.CountAsync();
            _currentPage = KeyboardPagination.GetValidPage(_currentPage, direction, totalCount);
            return await ShowReferenceList();
        }
        
        var reference = await _context.References.FirstOrDefaultAsync(r => r.Title == input);
        
        if (reference == null)
        {
            return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
        }

        var content = $"📚 {reference.Title}\n\n{reference.Description}";
        return new StateResult(content, StateAction.End);
    }
}
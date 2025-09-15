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

[State("Редактировать справку")]
[Description(0, "📋 Выбор справки для редактирования")]
[Description(1, "📝 Ввод нового заголовка")]
[Description(2, "📝 Ввод нового описания")]
public class EditReferenceState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private int _referenceId;
    private string? _newTitle;
    private int _currentPage = 0;

    public EditReferenceState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowReferenceSelection(),
            1 => await ProcessReferenceSelection(message),
            2 => await ProcessNewTitle(message),
            3 => await ProcessNewDescription(message),
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
            VkButtonColor.Primary);
        
        return new StateResult($"📋 Выберите справку для редактирования{pageInfo}:", StateAction.Stay, keyboard: keyboard);
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

        _referenceId = reference.Id;
        Step = 2;
        return new StateResult($"📝 Текущий заголовок: {reference.Title}\nВведите новый заголовок:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessNewTitle(UserMessage message)
    {
        _newTitle = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(_newTitle))
        {
            return new StateResult("❌ Заголовок не может быть пустым:", StateAction.Stay);
        }

        var reference = await _context.References.FindAsync(_referenceId);
        Step = 3;
        return new StateResult($"📄 Текущее описание:\n{reference?.Description}\n\n📝 Введите новое описание:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessNewDescription(UserMessage message)
    {
        var newDescription = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(newDescription))
        {
            return new StateResult("❌ Описание не может быть пустым:", StateAction.Stay);
        }

        var reference = await _context.References.FindAsync(_referenceId);
        if (reference != null)
        {
            reference.Title = _newTitle!;
            reference.Description = newDescription;
            await _context.SaveChangesAsync();
        }
        
        return new StateResult($"✅ Справка обновлена", StateAction.End);
    }
}
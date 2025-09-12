using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Application.States;

[State("Добавить справку")]
[Description(0, "📝 Ввод заголовка справки")]
[Description(1, "📝 Ввод описания справки")]
public class AddReferenceState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private string? _title;

    public AddReferenceState(AppDbContext context)
    { 
        _context = context;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => AskTitle(),
            1 => ProcessTitle(message),
            2 => await ProcessDescription(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private StateResult AskTitle()
    {
        Step = 1;
        return new StateResult("📝 Введите заголовок справки:", StateAction.Stay);
    }

    private StateResult ProcessTitle(UserMessage message)
    {
        _title = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(_title))
        {
            return new StateResult("❌ Заголовок не может быть пустым:", StateAction.Stay);
        }

        Step = 2;
        return new StateResult("📝 Введите описание справки:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessDescription(UserMessage message)
    {
        var description = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(description))
        {
            return new StateResult("❌ Описание не может быть пустым:", StateAction.Stay);
        }

        var reference = new Reference
        {
            Title = _title!,
            Description = description
        };
        
        _context.References.Add(reference);
        await _context.SaveChangesAsync();
        
        return new StateResult($"✅ Справка \"{_title}\" добавлена", StateAction.End);
    }
}
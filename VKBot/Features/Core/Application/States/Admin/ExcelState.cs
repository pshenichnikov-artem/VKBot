using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.States;

public class ExcelState : BaseState
{
    public ExcelState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => "Генерация Excel файла\nСоздает и отправляет Excel-файл со списком всех подтвержденных пользователей с информацией о группах и контактных данных";
    public override bool IsEntryPoint => true;
    public override string? Command => "/excel";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        if (message.ReplyToMessageId == null)
        {
            return StateResult.Success("Команда /excel должна быть ответом на сообщение", StateAction.End);
        }
        
        return StateResult.Success("Эксель файл", StateAction.End);
    }
}
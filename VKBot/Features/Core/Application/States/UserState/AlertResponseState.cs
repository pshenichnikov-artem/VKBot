using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using System.Text.Json;

namespace VKBot.Features.Core.Application.States;

public class AlertResponseState : BaseState
{
    private long _alertMessageId;

    public AlertResponseState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "Ответ на тревогу",
        1 => "Ввод количества студентов",
        _ => "Неизвестный шаг"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "Ответить";
    public override UserRole[] AllowedRoles => new[] { UserRole.Student };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ProcessAlertResponse(message),
            1 => await ProcessStudentCount(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ProcessAlertResponse(UserMessage message)
    {
        if (message.Payload == null 
            || !message.Payload.TryGetValue("messageId", out var messageIdElement)
            || !message.Payload.TryGetValue("type", out var type) 
            || type?.ToString() != PayloadType.AlertResponse.ToString())
        {
            return StateResult.Success("Недоступная функция", StateAction.End);
        }

        try
        {
            _alertMessageId = Convert.ToInt64(messageIdElement);
        }
        catch
        {
            return StateResult.Success("Ошибка обработки кнопки", StateAction.End);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var alertMsg = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == _alertMessageId && m.Payload != null && m.Payload.Contains("\"type\":\"alert\""));

        if (alertMsg == null)
        {
            return StateResult.Success("Сообщение не найдено", StateAction.End);
        }

        _step = 1;
        return StateResult.Success("Введите число студентов в укрытии (только 1 число от 0 до 50):", StateAction.Stay);
    }

    private async Task<StateResult> ProcessStudentCount(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (!int.TryParse(input, out var count) || count < 0 || count > 50)
        {
            return StateResult.Success("Введите корректное число от 0 до 50:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var responseMsg = new Message
        {
            SenderId = message.UserId,
            ReplyToMessageId = _alertMessageId,
            Payload = $"{{\"type\":\"{PayloadType.AlertResponse}\",\"count\":{count}}}"
        };
        context.Messages.Add(responseMsg);
        await context.SaveChangesAsync();

        return StateResult.Success($"Ваш ответ ({count} студентов) сохранен", StateAction.End);
    }
}
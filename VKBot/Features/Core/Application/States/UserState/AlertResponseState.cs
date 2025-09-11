using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using System.Text.Json;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State(PayloadType.AlertResponse, UserRole.Student)]
[Description(0, "🚨 Ответ на воздушную тревогу")]
[Description(1, "🔢 Введите количество студентов в укрытии")]
public class AlertResponseState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private long _alertMessageId;

    public AlertResponseState(AppDbContext context)
    { 
        _context = context;
    }



    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ProcessAlertResponse(message),
            1 => await ProcessStudentCount(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ProcessAlertResponse(UserMessage message)
    {
        if (message.Payload == null 
            || !message.Payload.TryGetValue("messageId", out var messageIdElement)
            || !message.Payload.TryGetValue("type", out var type) 
            || type?.ToString() != PayloadType.AlertResponse.ToString())
        {
            return new StateResult("❌ Недоступная функция", StateAction.End);
        }

        try
        {
            _alertMessageId = Convert.ToInt64(messageIdElement);
        }
        catch
        {
            return new StateResult("❌ Ошибка обработки кнопки", StateAction.End);
        }

        var alertMsg = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == _alertMessageId && m.Payload != null && m.Payload.Contains($"\"type\": \"{PayloadType.Alert}\""));

        if (alertMsg == null)
        {
            return new StateResult("❌ Сообщение не найдено", StateAction.End);
        }

        Step = 1;
        return new StateResult("🔢 Введите количество студентов в укрытии (от 0 до 50):", StateAction.Stay);
    }

    private async Task<StateResult> ProcessStudentCount(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (!int.TryParse(input, out var count) || count < 0 || count > 50)
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите число от 0 до 50:", StateAction.Stay);
        }

        var responseMsg = new Message
        {
            SenderId = message.UserId,
            ReplyToMessageId = _alertMessageId,
            Payload = $"{{\"type\":\"{PayloadType.AlertResponse}\",\"count\":{count}}}"
        };
        _context.Messages.Add(responseMsg);
        await _context.SaveChangesAsync();

        return new StateResult($"✅ Ответ сохранен: {count} студентов в укрытии", StateAction.End);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using System.Text.Json;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.Core.Application.States;

[State(PayloadType.AlertResponse, UserRole.Student)]
[Description(0, "🚨 Ответ на воздушную тревогу")]
[Description(1, "🔢 Университетская")]
[Description(2, "🔢 Гоголя")]
[Description(3, "🔢 Голландия")]
public class AlertResponseState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly ILogger<AlertResponseState> _logger;
    private long _alertMessageId;
    private int _universitetskayaCount;
    private int _gogolyaCount;
    private int _gollandiyaCount;

    public AlertResponseState(AppDbContext context, ILogger<AlertResponseState> logger)
    { 
        _context = context;
        _logger = logger;
    }



    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ProcessAlertResponse(message),
            1 => await ProcessUniversitetskayaCount(message),
            2 => await ProcessGogolyaCount(message),
            3 => await ProcessGollandiyaCount(message),
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
            _alertMessageId = Convert.ToInt64(messageIdElement.ToString());
            _logger.LogInformation("AlertMessageId получен из payload: {AlertMessageId}", _alertMessageId);
        }
        catch
        {
            return new StateResult("❌ Ошибка обработки кнопки", StateAction.End);
        }

        var alertMsg = await _context.Messages
            .FirstOrDefaultAsync(m => m.Payload != null && m.Payload.Contains($"\"type\":\"{PayloadType.Alert}\"") && m.Payload.Contains($"\"messageId\":{_alertMessageId}"));
        
        if (alertMsg == null)
        {
            return new StateResult("❌ Сообщение не найдено", StateAction.End);
        }
        
        var payload = JsonSerializer.Deserialize<JsonElement>(alertMsg.Payload);
        var deadline = DateTime.Parse(payload.GetProperty("deadline").GetString()!);
        
        if (DateTime.UtcNow > deadline)
        {
            return new StateResult("❌ Время для ответа истекло", StateAction.End);
        }

        Step = 1;
        return new StateResult("🔢 Сколько студентов на Университетской? (от 0 до 50):", StateAction.Stay);
    }

    private async Task<StateResult> ProcessUniversitetskayaCount(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (!int.TryParse(input, out var count) || count < 0 || count > 50)
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите число от 0 до 50:", StateAction.Stay);
        }

        _universitetskayaCount = count;
        Step = 2;
        return new StateResult("🔢 Сколько студентов на Гоголя? (от 0 до 50):", StateAction.Stay);
    }

    private async Task<StateResult> ProcessGogolyaCount(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (!int.TryParse(input, out var count) || count < 0 || count > 50)
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите число от 0 до 50:", StateAction.Stay);
        }

        _gogolyaCount = count;
        Step = 3;
        return new StateResult("🔢 Сколько студентов на Голландия? (от 0 до 50):", StateAction.Stay);
    }

    private async Task<StateResult> ProcessGollandiyaCount(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (!int.TryParse(input, out var count) || count < 0 || count > 50)
        {
            return new StateResult("❌ Неверный формат\n🔢 Введите число от 0 до 50:", StateAction.Stay);
        }

        _gollandiyaCount = count;
        
        var totalCount = _universitetskayaCount + _gogolyaCount + _gollandiyaCount;
        var payload = $"{{\"type\":\"{PayloadType.AlertResponse}\",\"universitetskaya\":{_universitetskayaCount},\"gogolya\":{_gogolyaCount},\"gollandiya\":{_gollandiyaCount},\"total\":{totalCount}}}";
        
        // Проверяем, есть ли уже ответ от этого пользователя
        var existingResponse = await _context.Messages
            .FirstOrDefaultAsync(m => m.SenderId == message.UserId && m.ReplyToMessageId == _alertMessageId);
        
        if (existingResponse != null)
        {
            // Обновляем существующий ответ
            existingResponse.Payload = payload;
        }
        else
        {
            // Создаем новый ответ
            var responseMsg = new Message
            {
                Id = message.MessageId,
                SenderId = message.UserId,
                ReplyToMessageId = _alertMessageId,
                Payload = payload
            };
            _context.Messages.Add(responseMsg);
        }
        
        await _context.SaveChangesAsync();

        var action = existingResponse != null ? "обновлен" : "сохранен";
        return new StateResult($"✅ Ответ {action}:\n🏢 Университетская: {_universitetskayaCount}\n🏢 Гоголя: {_gogolyaCount}\n🏢 Голландия: {_gollandiyaCount}\n🔢 Всего: {totalCount}", StateAction.End);
    }
}

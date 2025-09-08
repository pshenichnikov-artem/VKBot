using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Application.Interfaces;
using System.Text.Json;

namespace VKBot.Features.Core.Application.States.UserState;

public class EventResponseState : BaseState
{
    private List<Message> _events = new();
    private int _currentEventIndex = 0;

    public EventResponseState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "Ответ на событие",
        1 => "Выбор действия",
        2 => "Просмотр события",
        3 => "Введите ваш ответ",
        _ => "Неизвестный шаг"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "/respond";
    public override UserRole[] AllowedRoles => new[] { UserRole.Student };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowEventsList(message),
            1 => await ProcessMainAction(message),
            2 => await ProcessAction(message),
            3 => await ProcessResponse(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowEventsList(UserMessage message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var allEvents = await context.Messages
            .Include(m => m.Sender)
            .Where(m => m.Payload != null && m.Payload.Contains($"\"type\":\"{PayloadType.Event}\""))
            .OrderByDescending(m => m.Id)
            .ToListAsync();

        var events = allEvents.Where(evt => {
            var payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload!);
            if (payload.TryGetProperty("deadline", out var deadlineElement))
            {
                if (DateTime.TryParse(deadlineElement.GetString(), out var deadline))
                {
                    return DateTime.UtcNow <= deadline;
                }
            }
            return true;
        }).ToList();

        if (!events.Any())
        {
            return StateResult.Success("Нет событий", StateAction.End);
        }

        var eventsList = $"На данный момент актуальны {events.Count} событий";

        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Неотвеченные", VkButtonColor.Primary);
        keyboard.AddButton("Отвеченные", VkButtonColor.Secondary);
        keyboard.AddRow();
        keyboard.AddButton("Все события", VkButtonColor.Secondary);

        _step = 1;
        return StateResult.Success(eventsList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessMainAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        switch (action)
        {
            case "неотвеченные":
                await LoadEvents(message, "unanswered");
                break;
            case "отвеченные":
                await LoadEvents(message, "answered");
                break;
            case "все события":
                await LoadEvents(message, "all");
                break;
            default:
                return StateResult.Success("Неизвестное действие", StateAction.Stay);
        }
        
        if (_events.Count == 0)
        {
            return StateResult.Success("Нет событий для просмотра", StateAction.End);
        }
        
        _step = 2;
        _currentEventIndex = 0;
        return await ShowCurrentEvent(message);
    }

    private async Task<StateResult> ProcessAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();
        
        switch (action)
        {
            case "ответить":
            case "изменить ответ":
                _step = 3;
                return StateResult.Success("Введите ваш ответ:", StateAction.Stay);
            case "пропустить":
                break;
        }

        _currentEventIndex++;

        if (_currentEventIndex >= _events.Count)
        {
            return StateResult.Success("Все события просмотрены", StateAction.End);
        }

        return await ShowCurrentEvent(message);
    }

    private async Task<StateResult> ProcessResponse(UserMessage message)
    {
        var responseText = message.Text;
        if (string.IsNullOrEmpty(responseText))
        {
            return StateResult.Success("Ответ не может быть пустым:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentEvent = _events[_currentEventIndex];
        var delivery = await context.MessageDeliveries
            .FirstOrDefaultAsync(md => md.MessageId == currentEvent.Id && md.RecipientId == message.UserId);

        if (delivery != null)
        {
            delivery.DeliveryStatus = "responded";
        }

        var existingResponse = await context.Messages
            .FirstOrDefaultAsync(m => m.ReplyToMessageId == currentEvent.Id && m.SenderId == message.UserId);

        if (existingResponse != null)
        {
            existingResponse.Payload = $"{{\"type\":\"{PayloadType.EventResponse}\",\"text\":\"{responseText.Replace("\"", "\\\"")}\"}}";
        }
        else
        {
            var response = new Message
            {
                SenderId = message.UserId,
                ReplyToMessageId = currentEvent.Id,
                Payload = $"{{\"type\":\"{PayloadType.EventResponse}\",\"text\":\"{responseText.Replace("\"", "\\\"")}\"}}"
            };
            context.Messages.Add(response);
        }
        await context.SaveChangesAsync();

        _currentEventIndex++;
        _step = 2;

        if (_currentEventIndex >= _events.Count)
        {
            return StateResult.Success("Ваш ответ сохранен. Все события просмотрены", StateAction.End);
        }

        return await ShowCurrentEvent(message);
    }

    private async Task MarkAsRead(UserMessage message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentEvent = _events[_currentEventIndex];
        var delivery = await context.MessageDeliveries
            .FirstOrDefaultAsync(md => md.MessageId == currentEvent.Id && md.RecipientId == message.UserId);

        if (delivery != null)
        {
            delivery.isRead = true;
            await context.SaveChangesAsync();
        }
    }

    private async Task<StateResult> ShowCurrentEvent(UserMessage message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();

        if (_events.Count == 0)
        {
            await LoadEvents(message, "unanswered");
        }

        if (_currentEventIndex >= _events.Count)
        {
            return StateResult.Success("Нет событий для просмотра", StateAction.End);
        }

        var currentEvent = _events[_currentEventIndex];
        var payload = JsonSerializer.Deserialize<JsonElement>(currentEvent.Payload!);
        
        var title = payload.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : "Без заголовка";
        var deadline = DateTime.UtcNow;
        if (payload.TryGetProperty("deadline", out var deadlineElement))
        {
            DateTime.TryParse(deadlineElement.GetString(), out deadline);
        }

        var isExpired = DateTime.UtcNow > deadline;
        var existingResponse = context.Messages
            .FirstOrDefault(m => m.ReplyToMessageId == currentEvent.Id && m.SenderId == message.UserId);

        var eventInfo = $"Событие {_currentEventIndex + 1} из {_events.Count}: {title}\nОт: {currentEvent.Sender?.FullName}";
        
        var statusText = isExpired ? "\n⚠️ Время для ответов истекло" : $"\n⏰ Ответы до: {deadline.AddHours(3):dd.MM.yyyy HH:mm} МСК";
        var responseText = existingResponse != null ? "\n\nВаш ответ: " + GetResponseText(existingResponse) : "";

        await MarkAsRead(message);

        var keyboard = VkKeyboard.Create(false, true);
        if (!isExpired)
        {
            keyboard.AddRow();
            keyboard.AddButton(existingResponse != null ? "Изменить ответ" : "Ответить", VkButtonColor.Positive);
        }
        keyboard.AddRow();
        keyboard.AddButton("Пропустить", VkButtonColor.Secondary);

        await vkBot.ForwardMessageAsync(message.UserId, currentEvent.Id, eventInfo + statusText + responseText, keyboard);
        
        return StateResult.Success("", StateAction.Stay);
    }

    private async Task LoadEvents(UserMessage message, string filterType)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var allEvents = await context.Messages
            .Include(m => m.Sender)
            .Where(m => m.Payload != null && m.Payload.Contains($"\"type\":\"{PayloadType.Event}\""))
            .OrderBy(m => m.Id)
            .ToListAsync();

        _events = allEvents.Where(evt => {
            var payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload!);
            if (payload.TryGetProperty("deadline", out var deadlineElement))
            {
                if (DateTime.TryParse(deadlineElement.GetString(), out var deadline))
                {
                    if (DateTime.UtcNow > deadline) return false;
                }
            }
            
            var hasResponse = context.Messages
                .Any(m => m.ReplyToMessageId == evt.Id && m.SenderId == message.UserId);
                
            return filterType switch
            {
                "answered" => hasResponse,
                "unanswered" => !hasResponse,
                _ => true
            };
        }).ToList();
    }

    private string GetResponseText(Message response)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(response.Payload!);
        return payload.GetProperty("text").GetString() ?? "";
    }
}
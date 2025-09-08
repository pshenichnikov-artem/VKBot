using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using System.Text.RegularExpressions;

namespace VKBot.Features.Core.Application.States;

public class EventState : BaseState
{
    private string _targetType = "";
    private List<string> _targetGroups = new();

    public EventState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "📢 Создание события\nВыбор групп и отправке события всем в целевых групах",
        1 => "🎯 Выбор целевых групп",
        2 => _targetType == "groups" 
            ? "📝 Укажите группы"
            : "📝 Укажите потоки",
        3 => "📝 Введите заголовок",
        4 => "📎 Отправьте содержимое",
        5 => "⏰ Установка времени",
        _ => "❓ Неизвестный шаг"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "/event";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => ShowRecipientSelection(),
            1 => ProcessRecipientSelection(message),
            2 => await ProcessGroupSelection(message),
            3 => ProcessTitleStep(message),
            4 => ProcessEventTextStep(message),
            5 => await ProcessEventText(message),
            _ => StateResult.Success("❌ Ошибка", StateAction.End)
        };
    }

    private StateResult ShowRecipientSelection()
    {
        _step = 1;
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("🌍 Всем студентам", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("🎓 Потокам", VkButtonColor.Secondary);
        keyboard.AddButton("👥 Группам", VkButtonColor.Secondary);
        return StateResult.Success("🎯 Кому отправить событие?", StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessRecipientSelection(UserMessage message)
    {
        var selection = message.Text?.ToLower().Trim();
        switch (selection)
        {
            case var s when s.Contains("всем"):
                _targetType = "all";
                _step = 3;
                return StateResult.Success("📝 Отлично! Теперь введите заголовок события:", StateAction.Stay);
            case var s when s.Contains("потокам"):
                _targetType = "cohort";
                _step = 2;
                return StateResult.Success("🎓 Укажите названия потоков через запятую:\nПример: ПМ/б-22-1, ПМ/б-22-2", StateAction.Stay);
            case var s when s.Contains("группам"):
                _targetType = "groups";
                _step = 2;
                return StateResult.Success("👥 Укажите названия групп через запятую:\nПример: ПМ/б-22-1-о, ПМ/б-22-2-о", StateAction.Stay);
            default:
                return StateResult.Success("⚠️ Пожалуйста, используйте кнопки для выбора", StateAction.Stay);
        }
    }

    private async Task<StateResult> ProcessGroupSelection(UserMessage message)
    {
        var input = message.Text ?? "";
        if (_targetType == "groups")
        {
            var groupPattern = @"\b[А-Я]{2,3}/[а-я]-\d{2}-\d-[а-я]{1,2}\b";
            var matches = Regex.Matches(input, groupPattern);
            _targetGroups = matches.Select(m => m.Value).ToList();
        }
        else if (_targetType == "cohort")
        {
            var cohortPattern = @"\b[А-Я]{2,3}/[а-я]-\d{2}-[а-я]{1,2}\b";
            var matches = Regex.Matches(input, cohortPattern);
            _targetGroups = matches.Select(m => m.Value).ToList();
        }

        if (!_targetGroups.Any())
        {
            return StateResult.Success("❌ Неверный формат! Попробуйте ещё раз с правильным форматом", StateAction.Stay);
        }

        _step = 3;
        return StateResult.Success("✨ Отлично! Теперь введите заголовок события:", StateAction.Stay);
    }

    private string _eventTitle = "";
    private long _eventContentMessageId = 0;

    private StateResult ProcessTitleStep(UserMessage message)
    {
        _eventTitle = message.Text ?? "";
        if (string.IsNullOrEmpty(_eventTitle))
        {
            return StateResult.Success("⚠️ Заголовок не может быть пустым. Пожалуйста, введите заголовок:", StateAction.Stay);
        }

        _step = 4;
        return StateResult.Success("📎 Отлично! Теперь отправьте содержимое события (текст, фото, документы):", StateAction.Stay);
    }

    private StateResult ProcessEventTextStep(UserMessage message)
    {
        _eventContentMessageId = message.MessageId;
        _step = 5;
        return StateResult.Success("⏰ Отлично! Укажите сколько часов дать на ответ:\nПример: 24 (сутки), 72 (3 дня)", StateAction.Stay);
    }

    private async Task<StateResult> ProcessEventText(UserMessage message)
    {
        if (!int.TryParse(message.Text, out var hours) || hours <= 0)
        {
            return StateResult.Success("❌ Пожалуйста, введите корректное количество часов (целое число больше 0)", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recipients = await GetRecipients(context);

        var msg = new Message
        {
            Id = _eventContentMessageId,
            SenderId = message.UserId,
            Payload = $"{{\"type\":\"event\",\"title\":\"{_eventTitle.Replace("\"", "\\\"")}\",\"targets\":\"{string.Join(",", _targetGroups)}\",\"deadline\":\"{DateTime.UtcNow.AddHours(hours):yyyy-MM-ddTHH:mm:ssZ}\"}}"
        };
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            context.MessageDeliveries.Add(new MessageDelivery
            {
                MessageId = _eventContentMessageId,
                RecipientId = recipient.VkUserId,
                DeliveryStatus = MessageStatus.Pending.ToString().ToLower(),
                DispatchTime = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        return StateResult.Success($"✅ Событие успешно отправлено! 🎉\n\n👥 Получатели: {recipients.Count} чел.\n⏰ Время на ответ: {hours} час.", StateAction.End);
    }

    private async Task<List<User>> GetRecipients(AppDbContext context)
    {
        return _targetType switch
        {
            "all" => await context.Users.Where(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString()).ToListAsync(),
            "groups" => await context.Users.Include(u => u.Group)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                           _targetGroups.Contains(u.Group.Name) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            "cohort" => await context.Users.Include(u => u.Group)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                           _targetGroups.Any(cohort => u.Group.Name.StartsWith(cohort)) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            _ => new List<User>()
        };
    }
}
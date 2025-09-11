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
        0 => "📢 Создание нового события\nКоманда для создания и отправки событий студентам. Можно отправлять всем, конкретным потокам или группам. Устанавливается время для ответов.",
        1 => "👥 Выбор получателей\nИспользуйте кнопки для выбора аудитории",
        2 => _targetType == "groups" 
            ? "👥 Укажите группы\nФормат: ИТ/б-22-1-о, ИВТ/б-21-2-о (через запятую)"
            : "🎓 Укажите потоки\nФормат: ИТ/б-22-о, ИВТ/б-21-о (через запятую)",
        3 => "📝 Создание заголовка\nНапишите короткое название события",
        4 => "📄 Отправка содержимого\nОтправьте текст, фото или документ с описанием события",
        5 => "⏰ Установка времени\nУкажите количество часов для ответов (например: 24)",
        _ => "❌ Ошибка в процессе создания события"
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
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private StateResult ShowRecipientSelection()
    {
        _step = 1;
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Всем", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("Потокам", VkButtonColor.Secondary);
        keyboard.AddButton("Группам", VkButtonColor.Secondary);
        return StateResult.Success("📢 Выберите получателей события:", StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessRecipientSelection(UserMessage message)
    {
        var selection = message.Text?.ToLower().Trim();
        switch (selection)
        {
            case "всем":
                _targetType = "all";
                _step = 3;
                return StateResult.Success("📝 Введите заголовок события:", StateAction.Stay);
            case "потокам":
                _targetType = "cohort";
                _step = 2;
                return StateResult.Success("🎓 Введите потоки\nФормат: ИТ/б-22-о, ИВТ/б-21-о", StateAction.Stay);
            case "группам":
                _targetType = "groups";
                _step = 2;
                return StateResult.Success("👥 Введите группы\nФормат: ИТ/б-22-1-о, ИВТ/б-21-2-о", StateAction.Stay);
            default:
                return StateResult.Success("❌ Используйте кнопки для выбора", StateAction.Stay);
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
            return StateResult.Success("❌ Неверный формат\n📝 Пример: ИТ/б-22-1-о, ИВТ/б-21-2-о", StateAction.Stay);
        }

        _step = 3;
        return StateResult.Success("📝 Введите заголовок события:", StateAction.Stay);
    }

    private string _eventTitle = "";
    private long _eventContentMessageId = 0;

    private StateResult ProcessTitleStep(UserMessage message)
    {
        _eventTitle = message.Text ?? "";
        if (string.IsNullOrEmpty(_eventTitle))
        {
            return StateResult.Success("❌ Заголовок обязателен\n📝 Введите название события:", StateAction.Stay);
        }

        _step = 4;
        return StateResult.Success("📄 Отправьте текст события:", StateAction.Stay);
    }

    private StateResult ProcessEventTextStep(UserMessage message)
    {
        _eventContentMessageId = message.MessageId;
        _step = 5;
        return StateResult.Success("⏰ Введите количество часов для ответов:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessEventText(UserMessage message)
    {
        if (!int.TryParse(message.Text, out var hours) || hours <= 0)
        {
            return StateResult.Success("❌ Неверный формат\n⏰ Введите количество часов:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recipients = await GetRecipients(context);

        var msg = new Message
        {
            SenderId = message.UserId,
            Payload = $"{{\"type\":\"{PayloadType.Event}\",\"title\":\"{_eventTitle.Replace("\"", "\\\"")}\",\"targets\":\"{string.Join(",", _targetGroups)}\",\"deadline\":\"{DateTime.UtcNow.AddHours(hours):yyyy-MM-ddTHH:mm:ssZ}\"}}"
        };
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            context.MessageDeliveries.Add(new MessageDelivery
            {
                MessageId = msg.Id,
                RecipientId = recipient.VkUserId,
                DeliveryStatus = MessageStatus.Pending.ToString(),
                DispatchTime = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Excel", VkButtonColor.Primary, payload: $"{{\"type\":\"{PayloadType.Excel}\",\"messageId\":{msg.Id}}}");
        
        return StateResult.Success($"✅ Событие отправлено\n👥 Получателей: {recipients.Count}\n⏰ Время ответа: {hours}ч", StateAction.End, keyboard: keyboard);
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
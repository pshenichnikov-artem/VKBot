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
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State("Рассылка", UserRole.Admin)]
[Description(0, "📢 Создание нового события\nКоманда для создания и отправки событий студентам. Можно отправлять всем, конкретным потокам или группам. Устанавливается время для ответов.")]
[Description(1, "👥 Выбор получателей\nИспользуйте кнопки для выбора аудитории")]
[Description(2, "🎓 Укажите группу(Например ИТ/б-22-1-о) или поток(Например ИТ/б-22-о)")]
[Description(3, "📝 Создание заголовка\nНапишите короткое название события")]
[Description(4, "📄 Отправка содержимого\nОтправьте текст, фото или документ с описанием события")]
[Description(5, "⏰ Установка времени\nУкажите количество часов для ответов (например: 24)")]
public class EventState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    private string _targetType = "";
    private List<string> _targetGroups = new();

    public EventState(AppDbContext context)
    { 
        _context = context;
    }



    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => ShowRecipientSelection(),
            1 => await ProcessRecipientSelection(message),
            2 => await ProcessGroupSelection(message),
            3 => ProcessTitleStep(message),
            4 => ProcessEventTextStep(message),
            5 => await ProcessEventText(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private StateResult ShowRecipientSelection()
    {
        Step = 1;
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Всем", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("Потокам", VkButtonColor.Secondary);
        keyboard.AddButton("Группам", VkButtonColor.Secondary);
        keyboard.AddRow();
        keyboard.AddButton("По факультетам", VkButtonColor.Secondary);
        keyboard.AddButton("По форме обучения", VkButtonColor.Secondary);
        return new StateResult("📢 Выберите получателей события:", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessRecipientSelection(UserMessage message)
    {
        var selection = message.Text?.ToLower().Trim();
        switch (selection)
        {
            case "всем":
                _targetType = "all";
                Step = 3;
                return new StateResult("📝 Введите заголовок события:", StateAction.Stay);
            case "потокам":
                _targetType = "cohort";
                Step = 2;
                return new StateResult("🎓 Введите потоки\nФормат: ИТ/б-22-о, ИВТ/б-21-о", StateAction.Stay);
            case "группам":
                _targetType = "groups";
                Step = 2;
                return new StateResult("👥 Введите группы\nФормат: ИТ/б-22-1-о, ИВТ/б-21-2-о", StateAction.Stay);
            case "по факультетам":
                _targetType = "faculty";
                Step = 2;
                return await ShowFacultySelection();
            case "по форме обучения":
                _targetType = "studyform";
                Step = 2;
                return ShowStudyFormSelection();
            default:
                return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
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
        else if (_targetType == "faculty")
        {
            _targetGroups = input.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToList();
        }
        else if (_targetType == "studyform")
        {
            var form = input.Trim();
            if (!form.Equals("Очная", StringComparison.OrdinalIgnoreCase) && !form.Equals("Заочная", StringComparison.OrdinalIgnoreCase))
            {
                return ShowStudyFormSelection();
            }
            _targetGroups = new List<string> { form.Equals("Очная", StringComparison.OrdinalIgnoreCase) ? "Очная" : "Заочная" };
        }

        if (!_targetGroups.Any())
        {
            return new StateResult("❌ Неверный формат", StateAction.Stay);
        }

        Step = 3;
        return new StateResult("📝 Введите заголовок события:", StateAction.Stay);
    }

    private string _eventTitle = "";
    private long _eventContentMessageId = 0;

    private StateResult ProcessTitleStep(UserMessage message)
    {
        _eventTitle = message.Text ?? "";
        if (string.IsNullOrEmpty(_eventTitle))
        {
            return new StateResult("❌ Заголовок обязателен\n📝 Введите название события:", StateAction.Stay);
        }

        Step = 4;
        return new StateResult("📄 Отправьте текст события:", StateAction.Stay);
    }

    private StateResult ProcessEventTextStep(UserMessage message)
    {
        _eventContentMessageId = message.MessageId;
        Step = 5;
        return new StateResult("⏰ Введите количество часов для ответов:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessEventText(UserMessage message)
    {
        if (!int.TryParse(message.Text, out var hours) || hours <= 0)
        {
            return new StateResult("❌ Неверный формат\n⏰ Введите количество часов:", StateAction.Stay);
        }

        var recipients = await GetRecipients();

        var msg = new Message
        {
            SenderId = message.UserId,
            Payload = $"{{\"type\":\"{PayloadType.Event}\",\"title\":\"{_eventTitle.Replace("\"", "\\\"")}\",\"targets\":\"{string.Join(",", _targetGroups)}\",\"deadline\":\"{DateTime.UtcNow.AddHours(hours):yyyy-MM-ddTHH:mm:ssZ}\"}}"
        };
        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            _context.MessageDeliveries.Add(new MessageDelivery
            {
                MessageId = msg.Id,
                RecipientId = recipient.VkUserId,
                DeliveryStatus = MessageStatus.Pending.ToString(),
                DispatchTime = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Excel", VkButtonColor.Primary, payload: $"{{\"type\":\"{PayloadType.Excel}\",\"messageId\":{msg.Id}}}");
        
        return new StateResult($"✅ Событие отправлено\n👥 Получателей: {recipients.Count}\n⏰ Время ответа: {hours}ч", StateAction.End, keyboard: keyboard);
    }

    private async Task<List<User>> GetRecipients()
    {
        return _targetType switch
        {
            "all" => await _context.Users.Where(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString()).ToListAsync(),
            "groups" => await _context.Users.Include(u => u.Group)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                           _targetGroups.Contains(u.Group.Name) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            "cohort" => await _context.Users.Include(u => u.Group)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                           _targetGroups.Any(cohort => u.Group.Cohort.Contains(cohort)) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            "faculty" => await _context.Users.Include(u => u.Group).ThenInclude(g => g.Faculty)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && u.Group.Faculty != null &&
                           _targetGroups.Contains(u.Group.Faculty.Name) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            "studyform" => await _context.Users.Include(u => u.Group)
                .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null &&
                           _targetGroups.Contains(u.Group.StudyForm) && u.Role == UserRole.Student.ToString())
                .ToListAsync(),
            _ => new List<User>()
        };
    }
    
    private async Task<StateResult> ShowFacultySelection()
    {
        var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
        
        var facultiesList = "🏢 Доступные факультеты:\n";
        foreach (var faculty in faculties)
        {
            facultiesList += $"• {faculty.Name}\n";
        }
        facultiesList += "\nВведите названия через запятую:";
        
        return new StateResult(facultiesList, StateAction.Stay);
    }
    
    private StateResult ShowStudyFormSelection()
    {
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Очная", VkButtonColor.Primary);
        keyboard.AddButton("Заочная", VkButtonColor.Secondary);
        
        return new StateResult("🎓 Выберите форму обучения:", StateAction.Stay, keyboard: keyboard);
    }
}

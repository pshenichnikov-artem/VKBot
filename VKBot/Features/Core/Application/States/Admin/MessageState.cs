using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class MessageState : BaseState
{
    private string _targetType = "";
    private List<string> _targetGroups = new();

    public MessageState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "Отправка сообщения\nВыберите категорию получателей: Всем (все подтвержденные пользователи), Потокам (по коду потока), Группам (по коду группы)",
        1 => "Выбор получателей\nИспользуйте кнопки для выбора категории получателей",
        2 => _targetType == "groups" 
            ? "Ввод групп\nВведите коды групп в формате АБВ/а-12-3-б через запятую (например: ИВТ/б-22-1-о, ПИ/в-21-2-з)"
            : "Ввод потоков\nВведите коды потоков в формате АБВ/а-12-3 через запятую (например: ИВТ/б-22-1, ПИ/в-21-2)",
        3 => "Ввод текста сообщения\nВведите текст сообщения, которое будет отправлено выбранным получателям. Сообщение будет поставлено в очередь для отправки",
        _ => "Неизвестный шаг"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "/message";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => ShowRecipientSelection(),
            1 => ProcessRecipientSelection(message),
            2 => await ProcessGroupSelection(message),
            3 => await ProcessMessageText(message),
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

        return StateResult.Success("Выберите кому отправить сообщение:", StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessRecipientSelection(UserMessage message)
    {
        var selection = message.Text?.ToLower().Trim();
        
        switch (selection)
        {
            case "всем":
                _targetType = "all";
                _step = 3;
                return StateResult.Success("Введите текст сообщения:", StateAction.Stay);
                
            case "потокам":
                _targetType = "cohort";
                _step = 2;
                return StateResult.Success("Введите названия потоков через запятую:", StateAction.Stay);
                
            case "группам":
                _targetType = "groups";
                _step = 2;
                return StateResult.Success("Введите названия групп через запятую:", StateAction.Stay);
                
            default:
                return StateResult.Success("Неверный выбор. Используйте кнопки.", StateAction.Stay);
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
            return StateResult.Success("Неверный формат. Попробуйте еще раз:", StateAction.Stay);
        }
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notFoundGroups = await GetNotFoundGroups(context);
        
        if (notFoundGroups.Any())
        {
            return StateResult.Success($"{(_targetType == "groups" ? "Группы" : "Потоки")} не найдены: {string.Join(", ", notFoundGroups)}. Проверьте правильность ввода:", StateAction.Stay);
        }
        
        _step = 3;
        return StateResult.Success("Введите текст сообщения:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessMessageText(UserMessage message)
    {
        var messageText = message.Text;
        if (string.IsNullOrEmpty(messageText))
        {
            return StateResult.Success("Сообщение не может быть пустым. Введите текст:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recipients = await GetRecipients(context);

        var msg = new Message
        {
            SenderId = message.UserId,
            Payload = $"{{\"type\":\"{_targetType}\",\"targets\":\"{string.Join(",", _targetGroups)}\"}}"
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
                DispatchTime = DateTime.Now
            });
        }
        await context.SaveChangesAsync();

        return StateResult.Success($"Сообщение поставлено в очередь для {recipients.Count} получателей", StateAction.End);
    }

    private async Task<List<User>> GetRecipients(AppDbContext context)
    {
        return _targetType switch
        {
            "all" => await context.Users.Where(u => u.IsConfirmed && !u.IsBlocked).ToListAsync(),
            "groups" => await GetGroupRecipients(context),
            "cohort" => await GetCohortRecipients(context),
            _ => new List<User>()
        };
    }

    private async Task<List<User>> GetGroupRecipients(AppDbContext context)
    {
        return await context.Users.Include(u => u.Group)
            .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                       _targetGroups.Contains(u.Group.Name))
            .ToListAsync();
    }

    private async Task<List<User>> GetCohortRecipients(AppDbContext context)
    {
        return await context.Users.Include(u => u.Group)
            .Where(u => u.IsConfirmed && !u.IsBlocked && u.Group != null && 
                       _targetGroups.Any(cohort => u.Group.Name.StartsWith(cohort)))
            .ToListAsync();
    }

    private async Task<List<string>> GetNotFoundGroups(AppDbContext context)
    {
        if (_targetType == "groups")
        {
            var existingGroups = await context.Groups.Select(g => g.Name).ToListAsync();
            return _targetGroups.Where(g => !existingGroups.Contains(g)).ToList();
        }
        else if (_targetType == "cohort")
        {
            var existingGroups = await context.Groups.Select(g => g.Cohort).ToListAsync();
            return _targetGroups.Where(cohort => !existingGroups.Any(g => {
                var cohortParts = cohort.Split('-');
                if (cohortParts.Length < 3) return false;

                // Создаем паттерн: часть1-часть2-\d+-часть3
                var pattern = $"^{cohortParts[0]}-{cohortParts[1]}-\\d+-{cohortParts[2]}$";
                return Regex.IsMatch(g, pattern);
            })).ToList();
        }

        throw new ArgumentException();
    }
}
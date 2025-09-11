using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Application.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VKBot.Features.Core.Application.States.UserState;

public class RegistrationState : BaseState
{
    private string? _groupName;

    public RegistrationState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "🎓 Регистрация в системе университета\n🤖 Этот бот помогает студентам и администрации университета обмениваться важной информацией:\n\n🚨 Оповещения о воздушных тревогах\n📢 Уведомления о событиях и мероприятиях\n❓ Обращения к администрации\n📈 Отчеты и статистика\n\nДля регистрации укажите вашу группу и ФИО. После подтверждения администратором вы получите доступ ко всем функциям.",
        1 => "🎓 Укажите вашу группу\nВведите название вашей группы в формате ИТ/б-22-1-о.",
        2 => "👤 Укажите ваше ФИО\nВведите ваше полное ФИО в формате Иванов Иван Иванович.",
        _ => "❌ Ошибка в процессе регистрации"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "начать";
    public override UserRole[] AllowedRoles => new[] { UserRole.Unregistered };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await StartRegistration(message),
            1 => await ProcessGroup(message),
            2 => await ProcessFullName(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> StartRegistration(UserMessage message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

        if (existingUser != null)
        {
            if (existingUser.IsBlocked)
            {
                return StateResult.Success("🚫 Ваш аккаунт заблокирован\nОбратитесь к администрации", StateAction.End);
            }
            if (!existingUser.IsDeleted)
            {
                return StateResult.Success("✅ Вы уже зарегистрированы\n⏳ Ожидайте подтверждения", StateAction.End);
            }
        }

        _step = 1;
        return StateResult.Success("👋 Добро пожаловать в систему университета!\n\n🤖 Этот бот - официальная система связи между администрацией университета и студентами. Он обеспечивает быструю и надежную передачу важной информации.\n\n🔴 ОСНОВНЫЕ ФУНКЦИИ:\n\n🚨 ВОЗДУШНЫЕ ТРЕВОГИ\n• Мгновенные уведомления о сигналах тревоги\n• Отчеты о количестве студентов в укрытиях\n• Контроль безопасности студентов\n\n📢 СОБЫТИЯ И МЕРОПРИЯТИЯ\n• Уведомления о важных событиях\n• Обязательные опросы и анкетирования\n• Отслеживание ответов студентов\n\n❓ ОБРАЩЕНИЯ К АДМИНИСТРАЦИИ\n• Прямая связь с администрацией\n• Отправка вопросов и обращений\n• Получение официальных ответов\n\n📈 ОТЧЕТНОСТЬ И СТАТИСТИКА\n• Автоматические отчеты в Excel\n• Контроль прочтения сообщений\n• Напоминания о непрочитанных сообщениях\n\n🔒 БЕЗОПАСНОСТЬ\nВсе данные защищены и используются только для официальных целей университета.\n\n🎓 Для начала работы введите название вашей группы:\nФормат: ИТ/б-22-1-о", StateAction.Stay);
    }

    private async Task<StateResult> ProcessGroup(UserMessage message)
    {
        _groupName = message.Text?.Trim();

        if (string.IsNullOrEmpty(_groupName))
        {
            return StateResult.Success("❌ Название группы обязательно\n🎓 Пример: ИТ/б-22-1-о", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await context.Groups.FirstOrDefaultAsync(g => g.Name == _groupName);
        if (group == null)
        {
            return StateResult.Success("❌ Группа не найдена\n🎓 Проверьте название: ИТ/б-22-1-о", StateAction.Stay);
        }

        _step = 2;
        return StateResult.Success("👤 Введите ваше ФИО:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessFullName(UserMessage message)
    {
        var fullName = message.Text?.Trim();

        if (string.IsNullOrEmpty(fullName))
        {
            return StateResult.Success("❌ ФИО обязательно\n👤 Введите ваше ФИО:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await context.Groups.FirstAsync(g => g.Name == _groupName);
        var existingUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

        if (existingUser != null && existingUser.IsDeleted && !existingUser.IsBlocked)
        {
            // Восстанавливаем удаленного незаблокированного пользователя
            existingUser.FullName = fullName;
            existingUser.Role = UserRole.Unregistered.ToString();
            existingUser.IsConfirmed = false;
            existingUser.IsDeleted = false;
            existingUser.GroupId = group.Id;
        }
        else
        {
            // Создаем нового пользователя
            var user = new Domain.Entities.User
            {
                VkUserId = message.UserId,
                FullName = fullName,
                Role = UserRole.Unregistered.ToString(),
                IsConfirmed = false,
                IsBlocked = false,
                IsDeleted = false,
                GroupId = group.Id
            };
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        // Уведомляем администраторов о новой регистрации
        var admins = await context.Users.IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked)
            .ToListAsync();

        var notificationService = scope.ServiceProvider.GetRequiredService<UserNotificationService>();
        foreach (var admin in admins)
        {
            await notificationService.SendNewRegistrationNotification(admin.VkUserId, fullName, _groupName!);
        }

        return StateResult.Success("✅ Регистрация завершена\n⏳ Ожидайте подтверждения", StateAction.End);
    }


}
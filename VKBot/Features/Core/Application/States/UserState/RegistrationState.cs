using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VKBot.Features.Core.Application.States.UserState;

public class RegistrationState : BaseState
{
    private string? _groupName;

    public RegistrationState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "Регистрация студента",
        1 => "Ожидание ввода группы",
        2 => "Ожидание ввода ФИО",
        _ => "Неизвестный шаг"
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

        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

        if (existingUser != null)
        {
            return StateResult.Success("Вы уже зарегистрированы. Ожидайте подтверждения от администратора.", StateAction.End);
        }

        _step = 1;
        return StateResult.Success("Добро пожаловать! Для регистрации введите название вашей группы:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessGroup(UserMessage message)
    {
        _groupName = message.Text?.Trim();

        if (string.IsNullOrEmpty(_groupName))
        {
            return StateResult.Success("Название группы не может быть пустым. Введите название группы:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await context.Groups.FirstOrDefaultAsync(g => g.Name == _groupName);
        if (group == null)
        {
            return StateResult.Success("Группа не найдена. Введите корректное название группы:", StateAction.Stay);
        }

        _step = 2;
        return StateResult.Success("Введите ваше ФИО:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessFullName(UserMessage message)
    {
        var fullName = message.Text?.Trim();

        if (string.IsNullOrEmpty(fullName))
        {
            return StateResult.Success("ФИО не может быть пустым. Введите ваше ФИО:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var group = await context.Groups.FirstAsync(g => g.Name == _groupName);

        var user = new Domain.Entities.User
        {
            VkUserId = message.UserId,
            FullName = fullName,
            Role = UserRole.Unregistered.ToString(),
            IsConfirmed = false,
            IsBlocked = false,
            GroupId = group.Id
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return StateResult.Success("Регистрация завершена. Ожидайте подтверждения от администратора.", StateAction.End);
    }


}
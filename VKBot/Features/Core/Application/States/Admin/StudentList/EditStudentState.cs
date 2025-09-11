using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class EditStudentState : BaseState
{
    private string? _studentName;
    private long _studentId;
    private string? _newName;

    public EditStudentState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "✏️ Редактирование данных студента\nКоманда для изменения ФИО студента в системе.",
        1 => "🔍 Поиск студента\nВведите VK ID студента, данные которого нужно изменить.",
        2 => $"📝 Ввод нового ФИО\nУкажите новое ФИО для студента {_studentName}.",
        _ => "❌ Ошибка в процессе редактирования"
    };
  
    public override bool IsEntryPoint => false;
    public override string? Command => "изменить";

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override UserRole[] AllowedRoles => [UserRole.Admin];

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => RequestStudentName(),
            1 => await ProcessStudentName(message),
            2 => await ProcessNewName(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private StateResult RequestStudentName()
    {
        _step = 1;
        return StateResult.Success("🔍 Введите VK ID студента:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessStudentName(UserMessage message)
    {
        var input = message.Text?.Trim();
        if (string.IsNullOrEmpty(input) || !long.TryParse(input, out long vkId))
        {
            return StateResult.Success("❌ Неверный формат\n🔢 Введите корректный VK ID:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var student = await context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.VkUserId == vkId && u.Role == UserRole.Student.ToString() && u.IsConfirmed && !u.IsBlocked);
            
        if (student == null)
        {
            return StateResult.Success($"❌ Студент с VK ID {vkId} не найден\n🔍 Проверьте VK ID и попробуйте снова:", StateAction.Stay);
        }
        
        _studentName = $"{student.FullName}";
        _studentId = vkId;

        _step = 2;
        return StateResult.Success($"📝 Введите новое ФИО для {_studentName}:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessNewName(UserMessage message)
    {
        _newName = message.Text?.Trim();
        if (string.IsNullOrEmpty(_newName))
        {
            return StateResult.Success("❌ ФИО обязательно\n📝 Введите новое ФИО:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var student = await context.Users.FirstOrDefaultAsync(u => u.VkUserId == _studentId);
        if (student != null)
        {
            student.FullName = _newName;
            await context.SaveChangesAsync();
        }

        return StateResult.Success($"✅ ФИО студента успешно изменено\nСтарое: {_studentName}\nНовое: {_newName}", StateAction.End);
    }
}
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Enums;
using Group = VKBot.Features.Core.Domain.Entities.Group;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Domain.Models;
using System.Threading.Tasks;

namespace VKBot.Features.Core.Application.States;

public class GroupState : BaseState
{
    private string? _action;
    private string? _groupName;

    public GroupState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "👥 Управление группами",
        1 => "🎯 Выбор действия",
        2 => $"📝 Ввод названия группы",
        _ => "❓ Неизвестный шаг"
    };
    
    public override bool IsEntryPoint => true;
    public override string? Command => "/group";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowGroupMenu(),
            1 => await ProcessAction(message),
            2 => await ProcessGroupName(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowGroupMenu()
    {
        _step = 1;
        
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var groups = await context.Groups
            .OrderBy(g => g.Name)
            .ToListAsync();
            
        var groupsList = $"👥 Управление группами\n\n";
        
        VkKeyboard keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("➕ Добавить группу", VkButtonColor.Positive);
        if (!groups.Any())
        {
            groupsList += "😌 Группы пока не добавлены";
        }
        else
        {
            groupsList += $"📝 Список групп ({groups.Count}):\n";
            foreach (var group in groups)
            {
                groupsList += $"• {group.Name}\n";
            }
            keyboard.AddButton("❌ Удалить группу", VkButtonColor.Negative);
        }
        
        return StateResult.Success(groupsList, StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessAction(UserMessage message)
    {
        _action = message.Text?.ToLower().Trim();
        
        if (_action?.Contains("добавить") == true)
        {
            _step = 2;
            return StateResult.Success("➕ Введите название новой группы:\nПример: ПМ/б-22-1-пм", StateAction.Stay);
        }
        
        if (_action?.Contains("удалить") == true)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            if (!(await context.Groups.AnyAsync()))
            {
                return StateResult.Success("😌 Список групп пуст", StateAction.End);
            }
            
            _step = 2;
            return StateResult.Success("❌ Введите название группы для удаления:", StateAction.Stay);
        }
        
        return StateResult.Success("⚠️ Пожалуйста, используйте кнопки для выбора", StateAction.Stay);
    }

    private async Task<StateResult> ProcessGroupName(UserMessage message)
    {
        _groupName = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(_groupName))
        {
            return StateResult.Success("⚠️ Название группы не может быть пустым:", StateAction.Stay);
        }

        if (!IsValidGroupFormat(_groupName))
        {
            return StateResult.Success("❌ Неверный формат группы!\nПравильный формат: ПМ/б-22-1-пм", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var result = _action switch
        {
            "добавить" => await AddGroup(context, _groupName),
            "удалить" => await DeleteGroup(context, _groupName),
            _ => "Ошибка обработки действия"
        };

        return StateResult.Success(result, StateAction.End);
    }
    
    private async Task<string> AddGroup(AppDbContext context, string groupName)
    {
        var existingGroup = await context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
        if (existingGroup != null)
        {
            return $"⚠️ Группа {groupName} уже существует";
        }
        
        var (cohort, groupNumber) = ParseGroupName(groupName);
        
        var group = new Group 
        { 
            Name = groupName,
            Cohort = cohort,
            GroupNumber = groupNumber   
        };
        context.Groups.Add(group);
        await context.SaveChangesAsync();
        
        return $"✅ Группа {groupName} успешно добавлена! 🎉";
    }
    
    private async Task<string> DeleteGroup(AppDbContext context, string groupName)
    {
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
        if (group == null)
        {
            return $"❌ Группа {groupName} не найдена";
        }
        
        context.Groups.Remove(group);
        await context.SaveChangesAsync();
        
        return $"✅ Группа {groupName} успешно удалена";
    }

    private bool IsValidGroupFormat(string groupName)
    {
        var pattern = @"^[А-Я]{2,3}/[а-я]-\d{2}-\d+-[а-я]$";
        return Regex.IsMatch(groupName, pattern);
    }
    
    private (string cohort, short groupNumber) ParseGroupName(string groupName)
    {
        // ИТ/б-22-1-о -> Cohort: "ИТ/б-22", GroupNumber: 1
        var parts = groupName.Split('-');
        var cohort = string.Join("-", parts.Take(parts.Length - 2)); // ИТ/б-22
        var groupNumber = short.Parse(parts[^2]); // 1
        
        return (cohort, groupNumber);
    }
}
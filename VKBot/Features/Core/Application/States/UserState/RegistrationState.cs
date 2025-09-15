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
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Utils;

namespace VKBot.Features.Core.Application.States.UserState;

[State("Начать", UserRole.Unregistered)]
[Description(0, "🎓 Регистрация в системе")]
[Description(1, "🏛️ Выбор факультета")]
[Description(2, "👥 Выбор группы")]
[Description(3, "👤 Укажите ваше ФИО")]
public class RegistrationState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly UserNotificationService _notificationService;
    private int _facultyId;
    private long _groupId;
    private int _facultyPage = 0;
    private int _groupPage = 0;

    public RegistrationState(AppDbContext context, UserNotificationService notificationService)
    { 
        _context = context;
        _notificationService = notificationService;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await StartRegistration(message),
            1 => await ProcessFaculty(message),
            2 => await ProcessGroup(message),
            3 => await ProcessFullName(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> StartRegistration(UserMessage message)
    {
        var existingUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

        if (existingUser != null)
        {
            if (existingUser.IsBlocked)
            {
                return new StateResult("🚫 Ваш аккаунт заблокирован\nОбратитесь к администрации", StateAction.End);
            }
            if (!existingUser.IsDeleted)
            {
                return new StateResult("✅ Вы уже зарегистрированы\n⏳ Ожидайте подтверждения", StateAction.End);
            }
        }

        Step = 1;
        return await ShowFaculties();
    }

    private async Task<StateResult> ShowFaculties()
    {
        var faculties = await _context.Faculties.OrderBy(f => f.Name).ToListAsync();
        
        if (!faculties.Any())
        {
            return new StateResult("❌ Факультеты не настроены\nОбратитесь к администрации", StateAction.End);
        }
        
        var keyboard = KeyboardPagination.CreatePaginatedKeyboard(
            faculties, 
            _facultyPage, 
            f => f.Name, 
            out var pageInfo,
            VkButtonColor.Primary);
        
        return new StateResult($"👋 Добро пожаловать!\n🏛️ Выберите ваш факультет{pageInfo}:", StateAction.Stay, keyboard: keyboard);

    }
    
    private async Task<StateResult> ProcessFaculty(UserMessage message)
    {
        var input = message.Text?.Trim();
        
        if (KeyboardPagination.IsNavigationCommand(input, out var direction))
        {
            var totalCount = await _context.Faculties.CountAsync();
            var newPage = KeyboardPagination.GetValidPage(_facultyPage, direction, totalCount);
            if (newPage != _facultyPage)
            {
                _facultyPage = newPage;
                return await ShowFaculties();
            }
        }
        
        var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name.ToLower() == input.ToLower());
        
        if (faculty == null)
        {
            return new StateResult("❌ Используйте кнопки для выбора факультета", StateAction.Stay);
        }
        
        _facultyId = faculty.Id;
        _groupPage = 0;
        Step = 2;
        
        return await ShowGroups();
    }
    
    private async Task<StateResult> ShowGroups()
    {
        var groups = await _context.Groups
            .Where(g => g.FacultyId == _facultyId)
            .OrderBy(g => g.Name)
            .ToListAsync();
            
        if (!groups.Any())
        {
            return new StateResult("❌ В выбранном факультете нет групп\nОбратитесь к администрации", StateAction.End);
        }
        
        var keyboard = KeyboardPagination.CreatePaginatedKeyboard(
            groups, 
            _groupPage, 
            g => g.Name, 
            out var pageInfo,
            VkButtonColor.Primary);
        
        return new StateResult($"👥 Выберите вашу группу{pageInfo}:", StateAction.Stay, keyboard: keyboard);
    }
    
    private async Task<StateResult> ProcessGroup(UserMessage message)
    {
        var input = message.Text?.Trim();
        
        if (KeyboardPagination.IsNavigationCommand(input, out var direction))
        {
            var totalCount = await _context.Groups.CountAsync(g => g.FacultyId == _facultyId);
            var newPage = KeyboardPagination.GetValidPage(_groupPage, direction, totalCount);
            if (newPage != _groupPage)
            {
                _groupPage = newPage;
                return await ShowGroups();
            }
        }
        
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name.ToLower() == input.ToLower() && g.FacultyId == _facultyId);
        if (group == null)
        {
            return new StateResult("❌ Используйте кнопки для выбора группы", StateAction.Stay);
        }
        
        _groupId = group.Id;
        Step = 3;
        return new StateResult("👤 Введите ваше ФИО:", StateAction.Stay);
    }

    private async Task<StateResult> ProcessFullName(UserMessage message)
    {
        var fullName = message.Text?.Trim();

        if (string.IsNullOrEmpty(fullName))
        {
            return new StateResult("❌ ФИО обязательно\n👤 Введите ваше ФИО:", StateAction.Stay);
        }

        var group = await _context.Groups.FirstAsync(g => g.Id == _groupId);
        
        // Проверяем лимит студентов в группе
        var studentsInGroup = await _context.Users
            .Where(u => u.GroupId == _groupId && !u.IsDeleted)
            .CountAsync();
            
        if (studentsInGroup >= 2)
        {
            return new StateResult("❌ В данной группе уже зарегистрировано максимальное количество студентов (2)\nВыберите другую группу", StateAction.End);
        }
        
        var existingUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

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
            _context.Users.Add(user);
        }

        await _context.SaveChangesAsync();

        // Уведомляем администраторов о новой регистрации
        var admins = await _context.Users.IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked)
            .ToListAsync();

        foreach (var admin in admins)
        {
            await _notificationService.SendNewRegistrationNotification(admin.VkUserId, fullName, group.Name, message.UserId);
        }

        return new StateResult("✅ Регистрация завершена\n⏳ Ожидайте подтверждения", StateAction.End);
    }
}

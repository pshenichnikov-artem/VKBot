using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Application.Services;
using System.Threading.Tasks;
using System.Text.Json;

namespace VKBot.Features.Core.Application.States.Admin.Confirm;

[State(PayloadType.ConfirmUser, UserRole.Admin)]
public class ConfirmUserState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly UserNotificationService _notificationService;

    public ConfirmUserState(AppDbContext context, UserNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        if (message.Payload?.TryGetValue("userId", out var userIdElement) == true &&
            long.TryParse(userIdElement.ToString(), out var studentUserId))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkUserId == studentUserId && !u.IsConfirmed && !u.IsBlocked);
            
            if (user != null)
            {
                user.IsConfirmed = true;
                user.Role = UserRole.Student.ToString();
                await _context.SaveChangesAsync();
                
                // Уведомляем пользователя о подтверждении
                await _notificationService.SendUserConfirmedNotification(user.VkUserId);
                
                return new StateResult($"✅ Пользователь {user.FullName} подтвержден", StateAction.End);
            }
            else
            {
                return new StateResult("❌ Пользователь не найден или уже подтвержден", StateAction.End);
            }
        }
        
        return new StateResult("❌ Ошибка обработки запроса", StateAction.End);
    }
}
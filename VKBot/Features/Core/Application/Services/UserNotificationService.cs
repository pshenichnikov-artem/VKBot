using VKBot.Features.VK.Application.Interfaces;

namespace VKBot.Features.Core.Application.Services;

public class UserNotificationService
{
    private readonly IVkBot _vkBot;

    public UserNotificationService(IVkBot vkBot)
    {
        _vkBot = vkBot;
    }

    public async Task SendUserConfirmedNotification(long userId)
    {
        await _vkBot.SendMessageAsync(userId, "✅ Ваша регистрация подтверждена! Теперь вы можете пользоваться ботом.");
    }

    public async Task SendUserBlockedNotification(long userId)
    {
        await _vkBot.SendMessageAsync(userId, "🚫 Ваш аккаунт заблокирован администратором.");
    }

    public async Task SendUserUnblockedNotification(long userId)
    {
        await _vkBot.SendMessageAsync(userId, "✅ Ваш аккаунт разблокирован администратором.");
    }
}
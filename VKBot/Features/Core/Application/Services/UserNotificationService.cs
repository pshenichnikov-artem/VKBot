using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using VKBot.Features.Core.Domain.Enums;

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

    public async Task SendUserDeletedNotification(long userId)
    {
        await _vkBot.SendMessageAsync(userId, "🗑️ Ваш аккаунт удален администратором. Для повторной регистрации напишите 'начать'.");
    }

    public async Task SendNewRegistrationNotification(long adminId, string fullName, string groupName, long studentUserId)
    {
        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить", VkButtonColor.Primary, payload: "{\"type\":\"ConfirmUser\",\"userId\":" + studentUserId + "}");
        
        await _vkBot.SendMessageAsync(adminId, $"🎓 Новая регистрация\n\n👤 Студент: {fullName}\n🎓 Группа: {groupName}\n🔗 Профиль: [https://vk.com/id{studentUserId}|{studentUserId}]", keyboard: keyboard);
    }
}
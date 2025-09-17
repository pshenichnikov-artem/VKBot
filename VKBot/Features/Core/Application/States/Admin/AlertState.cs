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

[State("Тревога", UserRole.Admin)]
[Description(0, "Подтверждение отправки тревоги")]
[Description(1, "Отправка тревоги")]
public class AlertState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;

    public AlertState(AppDbContext context) 
    { 
        _context = context;
    }



    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowConfirmation(),
            1 => await ProcessConfirmation(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowConfirmation()
    {
        Step = 1;
        
        var studentsCount = await _context.Users
            .Include(u => u.Group)
            .CountAsync(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString() && u.Group!.StudyForm != "Заочная");
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить", VkButtonColor.Negative);
        keyboard.AddButton("Отмена", VkButtonColor.Secondary);
        
        return new StateResult($"⚠️ Подтвердите отправку тревоги\n\n👥 Получателей: {studentsCount} студентов\n⏰ Время на ответ: 2 часа", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> ProcessConfirmation(UserMessage message)
    {
        var input = message.Text?.Trim();
        
        if (input == "Подтвердить")
        {
            return await SendAlert(message);
        }
        
        if (input == "Отмена")
        {
            return new StateResult("❌ Отправка тревоги отменена", StateAction.End);
        }
        
        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Подтвердить", VkButtonColor.Negative);
        keyboard.AddButton("Отмена", VkButtonColor.Secondary);
        
        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay, keyboard: keyboard);
    }

    private async Task<StateResult> SendAlert(UserMessage message)
    {
        var recipients = await _context.Users
            .Include(u => u.Group)
            .Where(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString() && u.Group!.StudyForm != "Заочная")
            .ToListAsync();

        var msg = new Message
        {
            Id = message.MessageId, //????
            SenderId = null,
            Payload = null
        };
        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

        msg.Payload = $"{{\"type\":\"{PayloadType.Alert}\",\"messageId\":{msg.Id},\"deadline\":\"{DateTime.UtcNow.AddHours(5):yyyy-MM-ddTHH:mm:ssZ}\"}}";
        
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
        
        return new StateResult($"✅ Тревога отправлена\n👥 Получателей: {recipients.Count} студентов\n⏰ Время ответа: 2 часа", StateAction.End, keyboard: keyboard);
    }
}

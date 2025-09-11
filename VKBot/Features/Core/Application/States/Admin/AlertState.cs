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

[State("тревога", UserRole.Admin)]
[Description(0, "Воздушная тревога\n" +
    "Мгновенная отправка уведомления о воздушной тревоге всем студентам. Ответ только числом - количество студентов в укрытии. Время на ответ: 2 часа.")]
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
        return await SendAlert();
    }

    private async Task<StateResult> SendAlert()
    {
        var recipients = await _context.Users
            .Where(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString())
            .ToListAsync();

        var msg = new Message
        {
            SenderId = null,
            Payload = $"{{\"type\":\"{PayloadType.Alert}\",\"deadline\":\"{DateTime.UtcNow.AddHours(5):yyyy-MM-ddTHH:mm:ssZ}\"}}"
        };
        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

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

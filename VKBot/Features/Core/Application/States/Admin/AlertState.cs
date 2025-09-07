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

namespace VKBot.Features.Core.Application.States;

public class AlertState : BaseState
{


    public AlertState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => "Воздушная тревога\nМгновенная отправка уведомления о воздушной тревоге всем студентам. Ответ только числом - количество студентов в укрытии. Время на ответ: 2 часа.";

    public override bool IsEntryPoint => true;
    public override string? Command => "/alert";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return await SendAlert();
    }

    private async Task<StateResult> SendAlert()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var recipients = await context.Users
            .Where(u => u.IsConfirmed && !u.IsBlocked && u.Role == UserRole.Student.ToString())
            .ToListAsync();
            
        var alertText = "🚨 ВОЗДУШНАЯ ТРЕВОГА!\n\nНемедленно укройтесь в безопасном месте.\nОтветьте ТОЛЬКО ЧИСЛОМ - количество студентов в укрытии.\n\nВремя на ответ: 2 часа.";

        var msg = new Message
        {
            SenderId = null,
            Payload = $"{{\"type\":\"alert\",\"deadline\":\"{DateTime.UtcNow.AddHours(2):yyyy-MM-ddTHH:mm:ssZ}\"}}"
        };
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        foreach (var recipient in recipients)
        {
            context.MessageDeliveries.Add(new MessageDelivery
            {
                MessageId = msg.Id,
                RecipientId = recipient.VkUserId,
                DeliveryStatus = MessageStatus.Pending.ToString(),
                DispatchTime = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("Excel", VkButtonColor.Primary, payload: $"{{\"action\":\"excel\",\"messageId\":{msg.Id}}}");
        
        return StateResult.Success($"Тревога отправлена {recipients.Count} студентам!", StateAction.End, keyboard: keyboard);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.VK.Application.Interfaces;

namespace VKBot.Features.Host.BackgroundServices;

public class MessageDeliveryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageDeliveryService> _logger;

    public MessageDeliveryService(IServiceProvider serviceProvider, ILogger<MessageDeliveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages();
                await ProcessRetries();
                await ProcessReminders();
                await Task.Delay(30000, stoppingToken); // 30 секунд
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в MessageDeliveryService");
                await Task.Delay(60000, stoppingToken);
            }
        }
    }

    private async Task ProcessPendingMessages()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();
        var contentService = scope.ServiceProvider.GetRequiredService<MessageContentService>();

        var pendingDeliveries = await context.MessageDeliveries
            .Include(md => md.Message)
            .Where(md => md.DeliveryStatus == MessageStatus.Pending.ToString())
            .ToListAsync();

        foreach (var delivery in pendingDeliveries)
        {
            try
            {
                var result = await contentService.GenerateMessageContent(delivery.Message);
                var messageId = await vkBot.SendMessageAsync(delivery.RecipientId, result.Text, keyboard: result.Keyboard);

                if (messageId.HasValue)
                {
                    delivery.DeliveryStatus = MessageStatus.Sent.ToString();
                    delivery.SentAt = DateTime.UtcNow;
                    delivery.RetryCount = 0;
                }
                else
                {
                    delivery.DeliveryStatus = MessageStatus.Error.ToString();
                    delivery.RetryCount++;
                    delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(5 * delivery.RetryCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения {MessageId} пользователю {UserId}", 
                    delivery.MessageId, delivery.RecipientId);
                delivery.DeliveryStatus = MessageStatus.Error.ToString();
                delivery.RetryCount++;
                delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(5 * delivery.RetryCount);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task ProcessRetries()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();
        var contentService = scope.ServiceProvider.GetRequiredService<MessageContentService>();

        var failedDeliveries = await context.MessageDeliveries
            .Include(md => md.Message)
            .Where(md => md.DeliveryStatus == MessageStatus.Error.ToString() && 
                        md.RetryCount < 3 && 
                        md.NextRetryAt <= DateTime.UtcNow)
            .Take(5)
            .ToListAsync();

        foreach (var delivery in failedDeliveries)
        {
            try
            {
                var result = await contentService.GenerateMessageContent(delivery.Message);
                var messageId = await vkBot.SendMessageAsync(delivery.RecipientId, result.Text, keyboard: result.Keyboard);

                if (messageId.HasValue)
                {
                    delivery.DeliveryStatus = MessageStatus.Sent.ToString();
                    delivery.SentAt = DateTime.UtcNow;
                }
                else
                {
                    delivery.RetryCount++;
                    delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(10 * delivery.RetryCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка повторной отправки сообщения {MessageId}", delivery.MessageId);
                delivery.RetryCount++;
                delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(10 * delivery.RetryCount);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task ProcessReminders()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();

        var reminders = await context.MessageDeliveries
            .Include(md => md.Message)
            .Where(md => md.DeliveryStatus == MessageStatus.Sent.ToString() && 
                        !md.isRead && 
                        md.SentAt <= DateTime.UtcNow.AddHours(-2) &&
                        (md.LastReminderAt == null || md.LastReminderAt <= DateTime.UtcNow.AddHours(-4)))
            .Take(5)
            .ToListAsync();

        foreach (var delivery in reminders)
        {
            if (delivery.Message.Payload?.Contains($"\"type\":\"{PayloadType.Alert}\"") == true ||
                delivery.Message.Payload?.Contains($"\"type\":\"{PayloadType.Event}\"") == true ||
                delivery.Message.Payload?.Contains($"\"type\":\"{PayloadType.Question}\"") == true)
            {
                try
                {
                    await vkBot.SendMessageAsync(delivery.RecipientId, "⏰ Напоминание: у вас есть непрочитанное сообщение, требующее ответа.");
                    delivery.LastReminderAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки напоминания пользователю {UserId}", delivery.RecipientId);
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
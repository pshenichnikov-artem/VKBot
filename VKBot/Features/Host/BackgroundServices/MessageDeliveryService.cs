using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.VK.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace VKBot.Features.Host.BackgroundServices;

public class MessageDeliveryService : BackgroundService
{
    private readonly AppDbContext _context;
    private readonly IVkBot _vkBot;
    private readonly MessageContentService _contentService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MessageDeliveryService> _logger;

    public MessageDeliveryService(AppDbContext context, IVkBot vkBot, MessageContentService contentService, 
        IMemoryCache memoryCache, ILogger<MessageDeliveryService> logger)
    {
        _context = context;
        _vkBot = vkBot;
        _contentService = contentService;
        _memoryCache = memoryCache;
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
                await Task.Delay(5000, stoppingToken);
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
        var pendingDeliveries = await _context.MessageDeliveries
            .Include(md => md.Message)
            .Where(md => md.DeliveryStatus == MessageStatus.Pending.ToString())
            .ToListAsync();

        foreach (var delivery in pendingDeliveries)
        {
            var stateMachineKey = $"state_machine_{delivery.RecipientId}";
            if (_memoryCache.TryGetValue(stateMachineKey, out _))
            {
                continue;
            }

            try
            {
                var result = await _contentService.GenerateMessageContent(delivery.Message);
                var messageId = await _vkBot.SendMessageAsync(delivery.RecipientId, result.Text, keyboard: result.Keyboard);

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

        await _context.SaveChangesAsync();
    }

    private async Task ProcessRetries()
    {
        var failedDeliveries = await _context.MessageDeliveries
            .Include(md => md.Message)
            .Where(md => md.DeliveryStatus == MessageStatus.Error.ToString() && 
                        md.RetryCount < 3 && 
                        md.NextRetryAt <= DateTime.UtcNow)
            .Take(5)
            .ToListAsync();

        foreach (var delivery in failedDeliveries)
        {
            var stateMachineKey = $"state_machine_{delivery.RecipientId}";
            if (_memoryCache.TryGetValue(stateMachineKey, out _))
            {
                continue;
            }

            try
            {
                var result = await _contentService.GenerateMessageContent(delivery.Message);
                var messageId = await _vkBot.SendMessageAsync(delivery.RecipientId, result.Text, keyboard: result.Keyboard);

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

        await _context.SaveChangesAsync();
    }

    private async Task ProcessReminders()
    {
        var reminders = await _context.MessageDeliveries
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
                    await _vkBot.SendMessageAsync(delivery.RecipientId, "⏰ Напоминание\n📨 У вас есть непрочитанное сообщение");
                    delivery.LastReminderAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки напоминания пользователю {UserId}", delivery.RecipientId);
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
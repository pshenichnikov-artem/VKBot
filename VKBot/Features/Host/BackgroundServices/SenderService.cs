using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Application.Interfaces;

namespace VKBot.Features.Host.Services
{

    public class SenderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVkBot _vkBot;
        private readonly ILogger<SenderService> _logger;

        private const int DELAY_MS = 1000;

        public SenderService(IServiceProvider serviceProvider, IVkBot vkBot, ILogger<SenderService> logger)
        {
            _serviceProvider = serviceProvider;
            _vkBot = vkBot;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var threeHoursAgo = DateTime.Now.AddHours(-3);
                    var now = DateTime.Now;
                    var today10AM = new DateTime(now.Year, now.Month, now.Day, 10, 0, 0);
                    var yesterday10AM = today10AM.AddDays(-1);

                    var messages = await context.MessageDeliveries
                        .Include(md => md.Message)
                        .GroupJoin(context.Messages,
                                  md => md.Message!.Id,
                                  reply => reply.ReplyToMessageId,
                                  (md, replies) => new { md, hasReply = replies.Any() })
                        .Where(x => (x.md.DeliveryStatus == MessageStatus.Pending.ToString()
                                    && (x.md.DispatchTime == null || DateTime.Now >= x.md.DispatchTime.Value))
                                   || (x.md.Message!.ReplyToMessageId == null
                                      && x.md.DispatchTime.HasValue
                                      && x.md.DispatchTime.Value <= threeHoursAgo
                                      && !x.hasReply)
                                   || (x.md.Message!.EnableReminder
                                      && x.md.Message.ReplyToMessageId == null
                                      && !x.hasReply
                                      && now >= today10AM
                                      && (x.md.Message.LastReminderSent == null || x.md.Message.LastReminderSent < yesterday10AM)))
                        .ToListAsync();
                    
                    // Обновляем статус для повторной отправки
                    foreach (var item in messages.Where(x => !x.hasReply && x.md.DispatchTime.HasValue && x.md.DispatchTime.Value <= threeHoursAgo))
                    {
                        item.md.DeliveryStatus = MessageStatus.Pending.ToString();
                        item.md.DispatchTime = DateTime.Now;
                    }
                    
                    // Обновляем статус для напоминаний
                    foreach (var item in messages.Where(x => x.md.Message!.EnableReminder && !x.hasReply && now >= today10AM && (x.md.Message.LastReminderSent == null || x.md.Message.LastReminderSent < yesterday10AM)))
                    {
                        item.md.DeliveryStatus = MessageStatus.Pending.ToString();
                        item.md.DispatchTime = DateTime.Now;
                        item.md.Message!.LastReminderSent = DateTime.Now;
                    }
                    
                    var messagesToSend = messages.Where(x => x.md.DeliveryStatus == MessageStatus.Pending.ToString()).Select(x => x.md).ToList();

                    if (messagesToSend.Count == 0)
                    {
                        await Task.Delay(DELAY_MS);
                        continue;
                    }

                    var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                    foreach (var message in messagesToSend)
                    {
                        var sessionKey = $"state_machine_{message.RecipientId}";
                        
                        if (memoryCache.TryGetValue(sessionKey, out _))
                        {
                            continue;
                        }
                        
                        try
                        {
                            var messageId = await _vkBot.SendMessageAsync(message.RecipientId, "текст");
                            message.DeliveryStatus = MessageStatus.Delivered.ToString();
                            message.MessageId = messageId;
                            await context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Не удалось отправить сообщение {msg} пользователю {UserId} попытка {try} ошибка {ex}"
                                , message.Id, message.RecipientId, message.RetryCount, ex.Message);
                            message.RetryCount++;
                            if (message.RetryCount > 10)
                            {
                                message.DeliveryStatus = MessageStatus.Error.ToString();
                                _logger.LogWarning("Cообщение {msg} помещено в карантин пользователю {UserId} ошибка {ex}"
                                , message.Id, message.RecipientId, ex.Message);

                            }

                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                await Task.Delay(DELAY_MS);
            }

        }

    }
}

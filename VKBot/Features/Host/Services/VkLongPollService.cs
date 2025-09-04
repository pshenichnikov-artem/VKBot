using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Models;

namespace VKBot.Features.Host.Services;

public class VkLongPollService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IVkBot _vkBot;
    private readonly ILogger<VkLongPollService> _logger;

    public VkLongPollService(IServiceProvider serviceProvider, IVkBot vkBot, ILogger<VkLongPollService> logger)
    {
        _serviceProvider = serviceProvider;
        _vkBot = vkBot;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var server = await _vkBot.GetLongPollServerAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _vkBot.GetUpdatesAsync(server);

                foreach (var vkMessage in messages)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var messageProcessor = scope.ServiceProvider.GetRequiredService<IMessageProcessor>();

                    var userMessage = new UserMessage
                    {
                        UserId = vkMessage.FromId,
                        PeerId = vkMessage.PeerId,
                        MessageId = vkMessage.Id,
                        Text = vkMessage.Text,
                        ReplyToMessageId = vkMessage.ReplyMessage?.Id,
                        Attachments = vkMessage.Attachments.Select(a => new MessageAttachment
                        {
                            Type = a.Type,
                            Url = GetAttachmentUrl(a),
                            FileName = GetAttachmentFileName(a)
                        }).ToList()
                    };

                    _logger.LogInformation("[VkLongPoll] Получено сообщение от {UserId}, PeerId: {PeerId}, MessageId: {MessageId}", 
                        vkMessage.FromId, vkMessage.PeerId, vkMessage.Id);

                    var result = await messageProcessor.ProcessMessageAsync(userMessage);

                    if (!string.IsNullOrEmpty(result.Text))
                    {
                        _logger.LogInformation("[VkLongPoll] Отправляем ответ на PeerId: {PeerId}, ReplyTo: {ReplyTo}", 
                            vkMessage.PeerId, result.ReplyToMessageId);
                        await _vkBot.SendMessageAsync(vkMessage.PeerId, result.Text, result.ReplyToMessageId, result.Keyboard);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Long Poll");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
    
    private string GetAttachmentUrl(VkAttachmentItem attachment)
    {
        return attachment.Type switch
        {
            "photo" when attachment.Photo != null => attachment.Photo.Sizes.OrderByDescending(s => s.Width * s.Height).FirstOrDefault()?.Url ?? string.Empty,
            "doc" when attachment.Doc != null => attachment.Doc.Url,
            "video" when attachment.Video != null => attachment.Video.Player,
            _ => string.Empty
        };
    }
    
    private string GetAttachmentFileName(VkAttachmentItem attachment)
    {
        return attachment.Type switch
        {
            "photo" => "photo.jpg",
            "doc" when attachment.Doc != null => $"{attachment.Doc.Title}.{attachment.Doc.Extension}",
            "audio" when attachment.Audio != null => $"{attachment.Audio.Artist} - {attachment.Audio.Title}",
            "video" when attachment.Video != null => attachment.Video.Title,
            _ => $"Вложение типа {attachment.Type}"
        };
    }
}

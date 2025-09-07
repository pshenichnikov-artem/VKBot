using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.Host.Services;

public class VkLongPollService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VkLongPollService> _logger;

    public VkLongPollService(IServiceProvider serviceProvider, ILogger<VkLongPollService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LongPollServer? server = null;
        IVkBot? _vkBot = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_vkBot == null)
                {
                    var scope = _serviceProvider.CreateAsyncScope();
                    _vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();
                }

                if (server == null)
                {
                    server = await _vkBot.GetLongPollServerAsync();
                    if (server == null)
                    {
                        await Task.Delay(10000, stoppingToken);
                        continue;
                    }
                }
                
                var messages = await _vkBot.GetUpdatesAsync(server);

                foreach (var vkMessage in messages)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var stateMachineFactory = scope.ServiceProvider.GetRequiredService<IStateMachineFactory>();

                    var userMessage = new UserMessage
                    {
                        UserId = vkMessage.FromId,
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

                    _logger.LogInformation("[VkLongPoll] Получено сообщение от {UserId}: '{Text}'", 
                        vkMessage.FromId, vkMessage.Text);

                    var stateMachine = stateMachineFactory.GetOrCreate(vkMessage.FromId);
                    
                    var currentStateType = stateMachine.GetCurrentStateType();
                    var currentStateDescription = stateMachine.GetCurrentStateDescription();
                    
                    _logger.LogInformation("[VkLongPoll] Текущее состояние: {StateType} - {Description}", 
                        currentStateType?.Name ?? "None", currentStateDescription);
                    
                    var result = await stateMachine.ProcessMessage(userMessage);
                    
                    var newStateType = stateMachine.GetCurrentStateType();
                    if (newStateType != currentStateType)
                    {
                        var newStateDescription = stateMachine.GetCurrentStateDescription();
                        _logger.LogInformation("[VkLongPoll] Состояние изменено на: {StateType} - {Description}", 
                            newStateType?.Name ?? "None", newStateDescription);
                    }
                    else
                    {
                        _logger.LogInformation("[VkLongPoll] Состояние осталось: {StateType}", 
                            currentStateType?.Name ?? "None");
                    }

                    if (!string.IsNullOrEmpty(result.Text) || result.Keyboard != null || result.Attachments.Count > 0)
                    {
                        _logger.LogInformation("[VkLongPoll] Ответ бота: '{Text}'", result.Text);
                        await _vkBot.SendMessageAsync(vkMessage.PeerId, result.Text, result.ReplyToMessageId, result.Keyboard, result.Attachments);
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
    
    private long? GetAttachmentOwnerId(VkAttachmentItem attachment)
    {
        return attachment.Type switch
        {
            "photo" when attachment.Photo != null => attachment.Photo.OwnerId,
            "doc" when attachment.Doc != null => attachment.Doc.OwnerId,
            "video" when attachment.Video != null => attachment.Video.OwnerId,
            "audio" when attachment.Audio != null => attachment.Audio.OwnerId,
            _ => null
        };
    }
    
    private long? GetAttachmentMediaId(VkAttachmentItem attachment)
    {
        return attachment.Type switch
        {
            "photo" when attachment.Photo != null => attachment.Photo.Id,
            "doc" when attachment.Doc != null => attachment.Doc.Id,
            "video" when attachment.Video != null => attachment.Video.Id,
            "audio" when attachment.Audio != null => attachment.Audio.Id,
            _ => null
        };
    }
}

using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Interfaces;

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
                        UserId = vkMessage.UserId,
                        Text = vkMessage.Text,
                        ReplyToMessageId = vkMessage.ReplyToMessageId,
                        Attachments = vkMessage.Attachments.Select(a =>
                        {
                            var attachment = new MessageAttachment { Type = a.Type };

                            if (a.Payload.ValueKind == JsonValueKind.Object)
                            {
                                switch (a.Type)
                                {
                                    case "photo":
                                        if (a.Payload.TryGetProperty("sizes", out var sizes) && sizes.ValueKind == JsonValueKind.Array)
                                        {
                                            var largest = sizes.EnumerateArray()
                                                .OrderByDescending(s => s.GetProperty("width").GetInt32())
                                                .FirstOrDefault();

                                            attachment.Url = largest.GetProperty("url").GetString();
                                        }
                                        break;

                                    case "doc":
                                        attachment.Url = a.Payload.GetProperty("url").GetString();
                                        attachment.FileName = a.Payload.GetProperty("title").GetString();
                                        break;

                                    case "audio":
                                        attachment.FileName = $"{a.Payload.GetProperty("artist").GetString()} - {a.Payload.GetProperty("title").GetString()}";
                                        break;

                                    case "video":
                                        attachment.FileName = a.Payload.GetProperty("title").GetString();
                                        break;

                                    case "link":
                                        attachment.Url = a.Payload.GetProperty("url").GetString();
                                        attachment.FileName = a.Payload.GetProperty("title").GetString();
                                        break;

                                    default:
                                        break;
                                }
                            }
                            return attachment;
                        }).ToList()
                    };


                    var response = await messageProcessor.ProcessMessageAsync(userMessage);

                    if (!string.IsNullOrEmpty(response))
                    await _vkBot.SendMessageAsync(vkMessage.UserId, response);
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
}

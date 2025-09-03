using VKBot.Features.VK.Interfaces;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Models;

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
                        Attachments = vkMessage.Attachments.Select(a => new MessageAttachment
                        {
                            Type = a.Type,
                            Url = a.Url,
                            FileName = a.FileName
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

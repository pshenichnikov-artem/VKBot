using VKBot.Features.VK.Interfaces;
using VKBot.Features.Core.Application.Interfaces;

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
                
                foreach (var message in messages)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler>();
                    var commandMessage = await commandHandler.HandleAsync(message.UserId, message.Text);
                    if (!string.IsNullOrEmpty(commandMessage))
                        await _vkBot.SendMessageAsync(message.UserId, commandMessage);
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Application.Middleware;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Exceptions;

namespace VKBot.Features.Host.Services;

public partial class VkLongPollBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VkLongPollBackgroundService> _logger;
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _userLocks = new();

    public VkLongPollBackgroundService(IServiceProvider serviceProvider, ILogger<VkLongPollBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();

        LongPollServer? server = null;
        while (true)
        {
            try
            {
                if (server == null)
                {
                    server = await vkBot.GetLongPollServerAsync();
                    if (server == null)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                }

                var response = await vkBot.GetUpdatesAsync(server);

                if (response == null || response.Failed > 0)
                {
                    server = null;
                    continue;
                }

                server.Ts = response.Ts;

                if (response.Updates.ValueKind == JsonValueKind.Array)
                {
                    var updates = response.Updates.EnumerateArray().ToArray();
                    if (updates.Length > 0)
                    {
                        _logger.LogInformation("Получено {Count} событий", updates.Length);
                        var tasks = updates.Select(u => HandleUpdateAsync(u, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в Long Poll");
                server = null;
                await Task.Delay(5000);
            }
        }
    }

    private async Task HandleUpdateAsync(JsonElement update, CancellationToken token)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<Pipeline>();

            var context = new VkContext { Update = update };
            await pipeline.ExecuteAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки события");
        }
    }
}
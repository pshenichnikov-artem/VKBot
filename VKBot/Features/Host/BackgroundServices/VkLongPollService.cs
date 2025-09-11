using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Application.Services;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.VK.Application.Middleware;
using VKBot.Features.VK.Domain.Models;


namespace VKBot.Features.Host.Services;

public partial class VkLongPollService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VkLongPollService> _logger;
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _userLocks = new();

    public VkLongPollService(IServiceProvider serviceProvider, ILogger<VkLongPollService> logger)
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
        var userId = ExtractUserId(update);
        if (!userId.HasValue)
            return;

        var semaphore = _userLocks.GetOrAdd(userId.Value, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(token);
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<Pipeline>();

            var context = new VkContext { Update = update };
            await pipeline.ExecuteAsync(context);
            //TODO отправляем все VkResult
            foreach (var result in context.Results)
            {
                if (result.IsForwardMessage == true)
                {
                    //forward
                }
                else
                {
                    //нефорвад
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки апдейта для пользователя {UserId}", userId);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private long? ExtractUserId(JsonElement update)
    {
        try
        {
            if (update.ValueKind == JsonValueKind.Array)
            {
                var fields = update.EnumerateArray().ToArray();
                if (fields.Length >= 4 && fields[0].GetInt32() == 4) // событие "новое сообщение"
                {
                    return fields[3].GetInt64(); // userId
                }
            }
        }
        catch(Exception ex)
        {
            _logger.LogError("Не удалось получить userId {update} {ex}", update, ex.Message);
        }
        return null;
    }
}

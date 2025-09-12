using Microsoft.Extensions.Caching.Memory;
using VKBot.Features.Core.Data;
using VKBot.Features.VK.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public class AntiSpamMiddleware : MiddlewareBase
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;

    private const int MaxMessagesPerSecond = 5;

    public AntiSpamMiddleware(IMemoryCache cache, AppDbContext context, ILogger<AntiSpamMiddleware> logger) : base(logger)
    {
        _cache = cache;
        _context = context;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        _logger.LogInformation("→ INVOKE начало - проверка антиспам");
        
        if (context.Message == null)
        {
            await next();
            return;
        }

        var userId = context.Message.FromId;
        var messagesKey = $"spam_messages_{userId}";
        var blockKey = $"spam_block_{userId}";
        var banCountKey = $"spam_ban_count_{userId}";

        // Проверяем, заблокирован ли пользователь
        if (_cache.TryGetValue(blockKey, out _))
        {
            _logger.LogInformation("Пользователь {UserId} заблокирован за спам", userId);
            context.Results.Add(new VkResult
            {
                Text = "🚫 Вы заблокированы за спам. Подождите.",
                UserId = userId
            });
            return;
        }

        // Получаем счетчик сообщений за секунду
        if (!_cache.TryGetValue(messagesKey, out List<DateTime> messages))
        {
            messages = new List<DateTime>();
        }

        var now = DateTime.UtcNow;
        messages.RemoveAll(m => now - m > TimeSpan.FromSeconds(1));
        messages.Add(now);

        _cache.Set(messagesKey, messages, TimeSpan.FromSeconds(2));

        // Проверяем превышение лимита
        if (messages.Count > MaxMessagesPerSecond)
        {
            // Получаем количество предыдущих банов
            if (!_cache.TryGetValue(banCountKey, out int banCount))
            {
                banCount = 0;
            }

            banCount++;

            // Экспоненциальная блокировка: 5 сек, 1 мин, 5 мин, 30 мин, постоянно
            TimeSpan blockDuration = banCount switch
            {
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromMinutes(1),
                3 => TimeSpan.FromMinutes(5),
                4 => TimeSpan.FromMinutes(30),
                _ => TimeSpan.FromDays(365)
            };

            _cache.Set(blockKey, true, blockDuration);
            _cache.Set(banCountKey, banCount, TimeSpan.FromDays(1));

            if (banCount >= 5)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.VkUserId == userId);
                if (user != null)
                {
                    user.IsBlocked = true;
                    await _context.SaveChangesAsync();
                }
            }

            var blockMessage = banCount switch
            {
                1 => "🚫 Спам обнаружен! Блокировка на 5 секунд.",
                2 => "🚫 Повторный спам! Блокировка на 1 минуту.",
                3 => "🚫 Продолжаете спамить! Блокировка на 5 минут.",
                4 => "🚫 Последнее предупреждение! Блокировка на 30 минут.",
                _ => "🚫 Вы постоянно заблокированы за спам."
            };

            _logger.LogWarning("Пользователь {UserId} заблокирован за спам (попытка {BanCount})", userId, banCount);
            context.Results.Add(new VkResult
            {
                Text = blockMessage,
                UserId = userId
            });

            return;
        }

        await next();
        _logger.LogInformation("← INVOKE завершено - антиспам пройден");
    }
}
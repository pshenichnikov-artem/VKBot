using System.Text.Json;
using StackExchange.Redis;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Application.Interfaces;

namespace VKBot.Features.Core.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly IDatabase _database;
    private readonly TimeSpan _expiry = TimeSpan.FromMinutes(5);

    public SessionService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<UserSession?> GetSessionAsync(long vkUserId)
    {
        var key = $"session:{vkUserId}";
        var value = await _database.StringGetAsync(key);
        
        return value.HasValue 
            ? JsonSerializer.Deserialize<UserSession>(value) 
            : null;
    }

    public async Task SetSessionAsync(UserSession session)
    {
        var key = $"session:{session.UserId}";
        var value = JsonSerializer.Serialize(session);
        await _database.StringSetAsync(key, value, _expiry);
    }

    public async Task DeleteSessionAsync(long vkUserId)
    {
        var key = $"session:{vkUserId}";
        await _database.KeyDeleteAsync(key);
    }
}
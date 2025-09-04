using VKBot.Features.VK.Models;

namespace VKBot.Features.VK.Interfaces;

public interface IVkBot
{
    Task<LongPollServer?> GetLongPollServerAsync();
    Task<List<VkMessage>> GetUpdatesAsync(LongPollServer server);
    Task SendMessageAsync(long userId, string message);
}
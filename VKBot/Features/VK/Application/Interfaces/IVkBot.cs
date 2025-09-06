using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.VK.Application.Interfaces;

public interface IVkBot
{
    Task<LongPollServer?> GetLongPollServerAsync();
    Task<List<VkMessageItem>> GetUpdatesAsync(LongPollServer server);
    Task<long?> SendMessageAsync(long peerId, string message, long? replyToMessageId = null, VkKeyboard? keyboard = null, List<StateAttachment>? attachments = null);
}
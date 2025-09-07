using VKBot.Features.Core.Domain.Models;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.VK.Application.Interfaces;

public interface IVkBot
{
    Task<LongPollServer?> GetLongPollServerAsync();
    Task<List<VkMessageItem>> GetUpdatesAsync(LongPollServer server);
    Task<long?> SendMessageAsync(long peerId, string message, long? replyToMessageId = null, VkKeyboard? keyboard = null, List<StateAttachment>? attachments = null);
    Task<long?> ForwardMessageAsync(long peerId, long messageId, string? additionalMessage = null, VkKeyboard? keyboard = null);
    Task<long?> SendDocumentAsync(long peerId, string filePath, string message);
}
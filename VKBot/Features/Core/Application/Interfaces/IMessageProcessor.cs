using VKBot.Features.Core.Domain.Models;

namespace VKBot.Features.Core.Application.Interfaces
{
    public interface IMessageProcessor
    {
        Task<string?> ProcessMessageAsync(UserMessage message);
    }
}
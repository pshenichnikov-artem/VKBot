using VKBot.Features.Core.Domain.Models;

namespace VKBot.Features.Core.Application.Interfaces
{
    public interface IMessageProcessor
    {
        Task<StateResult?> ProcessMessageAsync(UserMessage message);
    }
}
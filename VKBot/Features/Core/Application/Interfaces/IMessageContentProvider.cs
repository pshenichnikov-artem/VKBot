using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Models;

namespace VKBot.Features.Core.Application.Interfaces;

public interface IMessageContentProvider
{
    string GetMessageType();
    Task<StateResult> GenerateMessageContent(Message message);
}
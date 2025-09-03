namespace VKBot.Features.Core.Application.Interfaces;

public interface ICommandHandler
{
    //TODO добавить messageId?
    Task<string> HandleAsync(long vkUserId, string message);
}
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Domain.Interfaces;

public interface IVKCommand
{
    IEnumerable<UserRole> UserRoles { get; }
    string CommandName { get; }// /start
    string Description { get; }
    //TODO проверка роли?
    //description
    IReadOnlyCollection<IVKCommandStep> Steps { get; }
    Task<string> ExecuteAsync(long vkUserId, string[] args);
}
using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Application.Interfaces;

public interface ISessionService
{
    Task<UserSession?> GetSessionAsync(long vkUserId);
    Task SetSessionAsync(UserSession session);
    Task DeleteSessionAsync(long vkUserId);
}
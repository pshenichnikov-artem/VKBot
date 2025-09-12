using VKBot.Features.VK.Domain.Models;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public abstract class MiddlewareBase
{
    protected readonly ILogger _logger;

    protected MiddlewareBase(ILogger logger)
    {
        _logger = logger;
    }

    public virtual async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        await next();
    }
}
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.VK.Application.Middleware;

public abstract class MiddlewareBase
{
    public virtual async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        await next();
    }

    public virtual async Task OnCallbackAsync(VkContext context, Func<Task> next)
    {
        await next();
    }
}
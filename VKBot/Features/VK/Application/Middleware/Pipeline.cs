using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.VK.Application.Middleware;

public class Pipeline
{
    private readonly List<(MiddlewareBase middleware, Func<VkContext, bool>? condition)> _middlewares = new();

    public void Use(MiddlewareBase middleware, Func<VkContext, bool>? condition = null)
    {
        _middlewares.Add((middleware, condition));
    }

    public async Task ExecuteAsync(VkContext context)
    {
        var activeMiddlewares = _middlewares
            .Where(m => m.condition?.Invoke(context) ?? true)
            .Select(m => m.middleware)
            .ToList();

        await ExecuteChain(context, activeMiddlewares, 0);
    }

    private async Task ExecuteChain(VkContext context, List<MiddlewareBase> middlewares, int index)
    {
        if (index >= middlewares.Count) return;

        var current = middlewares[index];
        await current.InvokeAsync(context, () => ExecuteChain(context, middlewares, index + 1));
        await ExecuteCallbackChain(context, middlewares, middlewares.Count - 1 - index);
    }

    private async Task ExecuteCallbackChain(VkContext context, List<MiddlewareBase> middlewares, int index)
    {
        if (index < 0) return;

        var current = middlewares[index];
        await current.OnCallbackAsync(context, () => ExecuteCallbackChain(context, middlewares, index - 1));
    }
}
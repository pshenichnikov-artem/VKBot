using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public class ExceptionMiddleware : MiddlewareBase
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (AuthorizationException ex)
        {
            _logger.LogWarning(ex, "Ошибка авторизации от пользователя {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "❌ Команда не найдена",
                UserId = context.Message?.FromId
            });
        }
        catch (ParseException ex)
        {
            _logger.LogError(ex, "Ошибка парсинга от пользователя {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "❌ Ошибка сервера",
                UserId = context.Message?.FromId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения от пользователя {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "❌ Ошибка сервера",
                UserId = context.Message?.FromId
            });
        }
    }
}
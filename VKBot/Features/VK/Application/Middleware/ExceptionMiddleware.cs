using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public class ExceptionMiddleware : MiddlewareBase
{
    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger) : base(logger)
    {
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (EventNotHandledException)
        {
            throw;
        }
        catch (CommandNotFoundException)
        {
            context.Results.Add(new VkResult
            {
                Text = "❌ Неизвестная команда",
                UserId = context.Message?.FromId
            });
            context.ShouldShowKeyboard = true;
        }
        catch (UserBlockedException)
        {
            _logger.LogWarning("Заблокированный пользователь {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "🚫 Ваш аккаунт заблокирован.\nОбратитесь к администрации.",
                UserId = context.Message?.FromId
            });
            context.ShouldShowKeyboard = false;
        }
        catch (AuthorizationException)
        {
            context.Results.Add(new VkResult
            {
                Text = "❌ Команда не найдена",
                UserId = context.Message?.FromId
            });
            context.ShouldShowKeyboard = true;
        }
        catch (ParseException ex)
        {
            _logger.LogError(ex, "Ошибка парсинга {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "❌ Ошибка сервера",
                UserId = context.Message?.FromId
            });
            context.ShouldShowKeyboard = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки {UserId}", context.Message?.FromId);
            context.Results.Add(new VkResult
            {
                Text = "❌ Ошибка сервера",
                UserId = context.Message?.FromId
            });
            context.ShouldShowKeyboard = true;
        }
    }
}
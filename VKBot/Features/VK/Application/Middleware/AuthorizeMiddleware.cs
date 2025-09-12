using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Application.States;
using System.Reflection;
using VKBot.Features.VK.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace VKBot.Features.VK.Application.Middleware;

public class AuthorizeMiddleware : MiddlewareBase
{
    private readonly AppDbContext _context;

    public AuthorizeMiddleware(AppDbContext context, ILogger<AuthorizeMiddleware> logger) : base(logger)
    {
        _context = context;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        _logger.LogInformation("→ INVOKE начало - проверка авторизации пользователя {UserId}", context.Message?.FromId);
        
        if (context.Message?.FromId != null)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.VkUserId == context.Message.FromId);
            
            context.User = user;
            
            if (user != null)
            {
                _logger.LogInformation("Пользователь найден: {Role}, Заблокирован: {IsBlocked}", user.Role, user.IsBlocked);
            }
            else
            {
                _logger.LogInformation("Пользователь не найден");
            }
            
            // Блокируем заблокированных пользователей
            if (user?.IsBlocked == true)
            {
                throw new UserBlockedException();
            }
            
            if (context.FoundState != null)
            {
                var stateAttribute = context.FoundState.GetType().GetCustomAttribute<StateAttribute>();
                if (stateAttribute?.IsEntryState == true && stateAttribute.AllowedRoles != null)
                {
                    var userRole = user != null ? Enum.Parse<UserRole>(user.Role) : UserRole.Unregistered;
                    
                    _logger.LogInformation("Проверка прав для роли {UserRole} на команду {StateType}", userRole, context.FoundState.GetType().Name);
                    
                    if (!stateAttribute.AllowedRoles.Contains(userRole))
                    {
                        _logger.LogWarning("Отказ в доступе пользователю {UserId} с ролью {UserRole}", context.Message.FromId, userRole);
                        throw new AuthorizationException("Недостаточно прав для выполнения команды");
                    }
                    
                    _logger.LogInformation("Доступ разрешен");
                }
            }
        }
        
        await next();
        _logger.LogInformation("← INVOKE завершено - авторизация пройдена");
    }
}
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
        
        if (context.Message?.FromId == null)
        {
            await next();
            return;
        }

        var user = await GetOrCreateUser(context.Message.FromId);
        context.User = user;

        if (user.IsBlocked)
        {
            throw new UserBlockedException();
        }

        var userRole = GetUserRole(user);
        
        SelectValidState(context, userRole);
        
        await next();
        _logger.LogInformation("← INVOKE завершено - авторизация пройдена");
    }
    
    private async Task<VKBot.Features.Core.Domain.Entities.User> GetOrCreateUser(long userId)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.VkUserId == userId);
            
        if (user == null)
        {
            user = new VKBot.Features.Core.Domain.Entities.User
            {
                VkUserId = userId,
                FullName = "Пользователь",
                Role = UserRole.Unregistered.ToString(),
                IsConfirmed = false,
                IsBlocked = false,
                IsDeleted = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Создан новый незарегистрированный пользователь {UserId}", userId);
        }
        
        return user;
    }
    
    private UserRole GetUserRole(VKBot.Features.Core.Domain.Entities.User user)
    {
        return (user.IsDeleted || user.Role == UserRole.Unregistered.ToString()) 
            ? UserRole.Unregistered 
            : Enum.Parse<UserRole>(user.Role);
    }
    
    private void SelectValidState(VkContext context, UserRole userRole)
    {
        bool hasAccess = false;

        if(context.FoundStates.Count == 0 && context.FoundState == null)
        {
            return;
        }
        
        if (context.FoundState == null)
        {
            var validState = context.FoundStates.FirstOrDefault(state =>
            {
                var stateAttribute = state.GetType().GetCustomAttribute<StateAttribute>();
                return stateAttribute?.AllowedRoles?.Contains(userRole) == true;
            });
            
            if (validState != null)
            {
                context.FoundState = validState;
                hasAccess = true;
                _logger.LogInformation("Выбрано состояние {StateType} для роли {UserRole}", validState.GetType().Name, userRole);
            }
        }
        else
        {
            var stateAttribute = context.FoundState.GetType().GetCustomAttribute<StateAttribute>();
            if (stateAttribute?.IsEntryState == true && stateAttribute.AllowedRoles != null)
            {
                hasAccess = stateAttribute.AllowedRoles.Contains(userRole);
            }
        }
        
        if (!hasAccess)
        {
            _logger.LogWarning("Отказ в доступе пользователю с ролью {UserRole}", userRole);
            throw new AuthorizationException("Недостаточно прав для выполнения команды");
        }
    }
}
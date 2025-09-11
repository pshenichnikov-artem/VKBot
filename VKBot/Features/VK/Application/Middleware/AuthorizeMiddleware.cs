using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Application.States;
using System.Reflection;
using VKBot.Features.VK.Application.Exceptions;

namespace VKBot.Features.VK.Application.Middleware;

public class AuthorizeMiddleware : MiddlewareBase
{
    private readonly AppDbContext _context;

    public AuthorizeMiddleware(AppDbContext context)
    {
        _context = context;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        if (context.Message?.FromId != null)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.VkUserId == context.Message.FromId);
            
            context.User = user;
            
            // Блокируем заблокированных пользователей
            if (user?.IsBlocked == true)
            {
                context.Results.Add(new VkResult
                {
                    Text = "🚫 Ваш аккаунт заблокирован.\nОбратитесь к администрации.",
                    UserId = context.Message.FromId
                });
                return;
            }
            
            if (context.FoundState != null)
            {
                var stateAttribute = context.FoundState.GetType().GetCustomAttribute<StateAttribute>();
                if (stateAttribute?.IsEntryState == true && stateAttribute.AllowedRoles != null)
                {
                    var userRole = user != null ? Enum.Parse<UserRole>(user.Role) : UserRole.Unregistered;
                    
                    if (!stateAttribute.AllowedRoles.Contains(userRole))
                    {
                        throw new AuthorizationException("Недостаточно прав для выполнения команды");
                    }
                }
            }
        }
        
        await next();
    }
}
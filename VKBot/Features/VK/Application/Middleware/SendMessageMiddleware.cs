using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Interfaces;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Application.States;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.Core.Enums;
using System.Reflection;
using VKBot.Features.VK.Enums;
using VKBot.Features.Core.Data;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.VK.Application.Exceptions;

namespace VKBot.Features.VK.Application.Middleware;

public class SendMessageMiddleware : MiddlewareBase
{
    private readonly IVkBot _vkBot;
    private readonly AppDbContext _context;

    public SendMessageMiddleware(IVkBot vkBot, AppDbContext context, ILogger<SendMessageMiddleware> logger) : base(logger)
    {
        _vkBot = vkBot;
        _context = context;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        try
        {
            await next();
        }catch(EventNotHandledException)
        {
            return;
        }
        
        if (context.Results.Any())
        {
            foreach (var result in context.Results)
            {
                if (result.UserId.HasValue && (!string.IsNullOrEmpty(result.Text) || result.Keyboard != null))
                {
                    if (result.IsForwardMessage && context.Message != null)
                    {
                        await _vkBot.ForwardMessageAsync(result.UserId.Value, context.Message.Id, result.Text, result.Keyboard);
                    }
                    else
                    {
                        await _vkBot.SendMessageAsync(result.UserId.Value, result.Text, keyboard: result.Keyboard, attachments: result.Attachments);
                    }
                }
            }
        }

        if (context.ShouldShowKeyboard && context.Message != null)
        {
            var user = context.User ?? await _context.Users
                .FirstOrDefaultAsync(u => u.VkUserId == context.Message.FromId);
                
            if (user != null)
            {
                var userRole = Enum.Parse<UserRole>(user.Role);
                var keyboard = CreateCommandsKeyboard(userRole);
                
                await _vkBot.SendMessageAsync(context.Message.FromId, "Выберите действие:", keyboard: keyboard);
            }
            else
            {
                var keyboard = VkKeyboard.Create(false, true);
                keyboard.AddRow();
                keyboard.AddButton("Начать", VkButtonColor.Primary);
                
                await _vkBot.SendMessageAsync(context.Message.FromId, "👋 Добро пожаловать! Нажмите кнопку для регистрации:", keyboard: keyboard);
            }
        }
    }

    private VkKeyboard CreateCommandsKeyboard(UserRole userRole)
    {
        var keyboard = VkKeyboard.Create(false, false);
            
        var commands = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseState)) && !t.IsAbstract)
            .Select(t => t.GetCustomAttribute<StateAttribute>())
            .Where(attr => attr?.IsEntryState == true &&
                          attr.AllowedRoles != null &&
                          attr.AllowedRoles.Contains(userRole) &&
                          !string.IsNullOrEmpty(attr.Command))
            .Select(attr => attr.Command!)
            .OrderBy(cmd => cmd)
            .ToList();

        for (int i = 0; i < commands.Count; i++)
        {
            if (i % 2 == 0) keyboard.AddRow();
            keyboard.AddButton(commands[i], VkButtonColor.Primary);
        }

        return keyboard;
    }
}
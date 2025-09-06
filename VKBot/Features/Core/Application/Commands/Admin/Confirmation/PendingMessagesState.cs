using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;

namespace VKBot.Features.Core.Application.Commands.Admin.Confirmation
{
    public class PendingMessagesState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public PendingMessagesState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("Подтвердить всех", UserRole.Admin), typeof(PendingMessagesState) },
            { ("Подтвердить пользователя", UserRole.Admin), typeof(ConfirmUserState) }
        };

        protected override async Task<(StateDecorator?, StateResult)?> ValidateTransitionAsync(UserMessage message, UserSession session)
        {
            if (message.Text == "Подтвердить всех")
            {
                var result = await ExecuteAsync(message, session);
                return (null, result);
            }
            return null;
        }

        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            if (message.Text == "Подтвердить всех")
            {
                await _context.Users
                    .Where(u => !u.IsConfirmed && !u.IsBlocked)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.IsConfirmed, true));
                    
                return StateResult.Success("Все пользователи подтверждены.");
            }
          
            var pendingUsers = await _context.Users
                .Include(u => u.Group)
                .Where(u => !u.IsConfirmed && !u.IsBlocked)
                .ToListAsync();
                
            if (!pendingUsers.Any())
                return StateResult.Success("Нет пользователей, ожидающих подтверждения.");
                
            var usersList = string.Join("\n", pendingUsers.Select(u => $"{u.FullName} - {u.Group?.Name}"));

            var keyboard = CreateKeyboardFromCommands(message.UserId);

            return StateResult.Success($"Пользователи, ожидающие подтверждения:\n{usersList}\n\nНажмите 'Подтвердить всех' или 'Подтвердить пользователя'", keyboard: keyboard);
        }
    }
}
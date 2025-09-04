using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.Admin.Confirmation
{
    public class ConfirmUserState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public ConfirmUserState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("Закончить", UserRole.Admin), typeof(BaseState) },
            { ("*", UserRole.Admin), typeof(ConfirmUserState) }
        };
        
        protected override async Task<(StateDecorator?, StateResult)?> ValidateTransitionAsync(UserMessage message, UserSession session)
        {
            if (message.Text == "Закончить")
                return (null, StateResult.Success("Подтверждение завершено."));
                
            var result = await ExecuteAsync(message, session);
            if (!result.IsSuccess)
            {
                var hasMoreUsers = await _context.Users.AnyAsync(u => !u.IsConfirmed && !u.IsBlocked);
                return hasMoreUsers ? (this, result) : (null, StateResult.Success("Нет больше пользователей для подтверждения."));
            }
                
            var hasMore = await _context.Users.AnyAsync(u => !u.IsConfirmed && !u.IsBlocked);
            return hasMore ? (this, result) : (null, result);
        }
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var userName = message.Text;
            
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.FullName.Contains(userName) && !u.IsConfirmed && !u.IsBlocked);
                
            if (user == null)
                return StateResult.Failure("Пользователь не найден. Введите имя пользователя или 'Закончить':");
                
            user.IsConfirmed = true;
            await _context.SaveChangesAsync();
            
            return StateResult.Success($"Пользователь {user.FullName} подтвержден.\nВведите имя следующего пользователя или 'Закончить':");
        }
    }
}
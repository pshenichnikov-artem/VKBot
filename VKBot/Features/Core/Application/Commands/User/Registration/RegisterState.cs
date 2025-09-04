using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User.Registration
{
    public class RegisterState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public RegisterState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", null), typeof(EnterNameState) }
        };

        protected override async Task<(StateDecorator?, StateResult)?> ValidateTransitionAsync(UserMessage message, UserSession session)
        {
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

            if (user?.IsConfirmed == false)
            {
                return (null, StateResult.Success("Ожидайте подтверждения администратора."));
            }

            if (user?.IsBlocked == true)
            {
                return (null, StateResult.Success("Вы заблокированы."));
            }

            return null;
        }

        protected override Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            return Task.FromResult(StateResult.Success("Введите ваше ФИО:"));
        }
    }
}
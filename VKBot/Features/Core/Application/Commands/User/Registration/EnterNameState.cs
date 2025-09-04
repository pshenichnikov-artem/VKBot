using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User.Registration
{
    public class EnterNameState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public EnterNameState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", null), typeof(EnterGroupState) }
        };

        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var name = message.Text;
            var user = await _context.Users.Include(u => u.Group).FirstOrDefaultAsync(u => u.VkUserId == message.UserId);

            if (user?.Group != null)
            {
                return StateResult.Success($"Вы уже закреплены за группой {user.Group.Name}.");
            }

            session.Data["name"] = name;
            return StateResult.Success("Введите вашу группу:");
        }
    }
}
using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.Commands.User.Registration
{
    public class EnterGroupState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public EnterGroupState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var groupName = message.Text;
            
            if (!await IsValidGroup(groupName))
            {
                return StateResult.Failure("Группа не найдена или уже занята. Введите корректную группу:");
            }
            
            var name = session.Data["name"].ToString();
            await CreateOrUpdateUser(message.UserId, name, groupName);
            
            return StateResult.Success($"Регистрация завершена!\nИмя: {name}\nГруппа: {groupName}\nОжидайте подтверждения администратора.");
        }
        
        private async Task<bool> IsValidGroup(string groupName)
        {
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group == null) return false;
            
            var userExists = await _context.Users.AnyAsync(u => u.GroupId == group.Id);
            return !userExists;
        }
        
        private async Task CreateOrUpdateUser(long vkId, string name, string groupName)
        {
            var group = await _context.Groups.FirstAsync(g => g.Name == groupName);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VkUserId == vkId);
            
            if (user != null)
            {
                user.GroupId = group.Id;
                user.IsConfirmed = false;
            }
            else
            {
                user = new Domain.Entities.User
                {
                    VkUserId = vkId,
                    FullName = name,
                    Role = "Student",
                    GroupId = group.Id,
                    IsConfirmed = false
                };
                _context.Users.Add(user);
            }
            
            await _context.SaveChangesAsync();
        }
    }
}
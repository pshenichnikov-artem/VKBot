using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace VKBot.Features.Core.Application.Commands.Admin.Students
{
    public class ChangeLeaderState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public ChangeLeaderState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var newName = message.Text.Trim();
            var groupName = session.Data["selectedGroup"].ToString();
            
            var group = await _context.Groups
                .Include(g => g.Users)
                .FirstOrDefaultAsync(g => g.Name == groupName);
                
            if (group == null)
                return StateResult.Failure("Группа не найдена.");
                
            var leader = group.Users.FirstOrDefault();
            if (leader == null)
                return StateResult.Failure($"В группе {groupName} нет старосты.");
                
            var oldName = leader.FullName;
            leader.FullName = newName;
            await _context.SaveChangesAsync();
            
            return StateResult.Success($"Имя старосты группы {groupName} изменено: {oldName} -> {newName}");
        }
    }
}
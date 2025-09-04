using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace VKBot.Features.Core.Application.Commands.Admin.Groups
{
    public class DeleteGroupState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public DeleteGroupState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var groupName = message.Text.Trim();
            
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group == null)
                return StateResult.Failure($"Группа '{groupName}' не найдена.");
                
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            
            return StateResult.Success($"Группа '{groupName}' успешно удалена.");
        }
    }
}
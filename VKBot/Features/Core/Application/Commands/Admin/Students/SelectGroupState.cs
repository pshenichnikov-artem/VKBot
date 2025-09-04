using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace VKBot.Features.Core.Application.Commands.Admin.Students
{
    public class SelectGroupState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public SelectGroupState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("*", UserRole.Admin), typeof(EnterNewLeaderNameState) }
        };
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var groups = await _context.Groups
                .Include(g => g.Users)
                .ToListAsync();
                
            if (!groups.Any())
                return StateResult.Failure("Нет групп.");
                
            var result = "Выберите группу для изменения старосты:\n\n";
            
            foreach (var group in groups)
            {
                var leader = group.Users.FirstOrDefault();
                result += $"Группа: {group.Name}\n";
                result += $"Текущий староста: {leader?.FullName ?? "Не назначен"}\n\n";
            }
            
            result += "Введите название группы:";
            
            return StateResult.Success(result);
        }
    }
}
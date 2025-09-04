using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Application.Commands.Admin.Students;
using Microsoft.EntityFrameworkCore;

namespace VKBot.Features.Core.Application.Commands
{
    public class StudentsListState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public StudentsListState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new()
        {
            { ("Удалить", UserRole.Admin), typeof(EnterStudentNameState) },
            { ("Изменить", UserRole.Admin), typeof(SelectGroupState) }
        };
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var groups = await _context.Groups
                .Include(g => g.Users)
                .ToListAsync();
                
            if (!groups.Any())
                return StateResult.Success("Нет групп.");
                
            var result = "Список студентов:\n\n";
            
            foreach (var group in groups)
            {
                var leader = group.Users.FirstOrDefault();
                result += $"{group.Name}" + $"{leader?.FullName ?? "Не назначен"} (ID:)\n";
            }
            
            result += "Нажмите 'Удалить' или 'Изменить'";
            
            return StateResult.Success(result);
        }
    }
}
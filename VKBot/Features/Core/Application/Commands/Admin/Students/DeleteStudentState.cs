using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace VKBot.Features.Core.Application.Commands.Admin.Students
{
    public class DeleteStudentState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public DeleteStudentState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var studentName = message.Text.Trim();
            
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.FullName.Contains(studentName));
                
            if (student == null)
                return StateResult.Failure($"Студент '{studentName}' не найден. Повторите ввода");
                
            _context.Users.Remove(student);
            await _context.SaveChangesAsync();
            
            return StateResult.Success($"Студент '{student.FullName}' удален.");
        }
    }
}
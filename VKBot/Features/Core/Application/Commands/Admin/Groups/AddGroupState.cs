using VKBot.Features.Core.Domain.Interfaces;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace VKBot.Features.Core.Application.Commands.Admin.Groups
{
    public class AddGroupState : StateDecorator
    {
        private readonly AppDbContext _context;
        
        public AddGroupState(AppDbContext context)
        {
            _context = context;
        }
        
        protected override Dictionary<(string command, UserRole? role), Type> Transitions => new();
        
        protected override bool IsLast => true;
        
        protected override async Task<StateResult> ExecuteAsync(UserMessage message, UserSession session)
        {
            var groupName = message.Text.Trim();
            
            if (!IsValidGroupName(groupName))
                return StateResult.Failure("Неверный формат группы. Ожидается: XX/б-YY-Z-о или XX/м-YY-Z-з");
            
            if (await _context.Groups.AnyAsync(g => g.Name == groupName))
                return StateResult.Failure($"Группа '{groupName}' уже существует.");
                
            var (cohort, groupNumber) = ParseGroupName(groupName);
            var group = new VKBot.Features.Core.Domain.Entities.Group 
            { 
                Name = groupName,
                Cohort = cohort,
                GroupNumber = groupNumber
            };
            
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            
            return StateResult.Success($"Группа '{groupName}' успешно добавлена.\nПоток: {cohort}\nНомер группы: {groupNumber}");
        }
        
        private static bool IsValidGroupName(string name)
        {
            return Regex.IsMatch(name, @"^[А-Я]{2,3}/[бм]-\d{2}-\d-[оз]$");
        }
        
        private static (string cohort, short groupNumber) ParseGroupName(string name)
        {
            var match = Regex.Match(name, @"^([А-Я]{2,3}/[бм]-\d{2})-\d-([оз])$");
            var cohort = match.Groups[1].Value + "-" + match.Groups[2].Value;
            
            var numberMatch = Regex.Match(name, @"-\d{2}-(\d)-");
            var groupNumber = short.Parse(numberMatch.Groups[1].Value);
            
            return (cohort, groupNumber);
        }
    }
}
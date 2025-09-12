using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.States.UserState
{
    [State("Вопрос", UserRole.Student)]
    [Description(0, "Выбор администратора")]
    [Description(1, "📝 Напишите вопрос")]
    [Description(2, "Отправка вопроса")]
    public class QuestionState : BaseState
    {
        [NonSerialized]
        private readonly AppDbContext _context;
        private long _selectedAdminId;

        public QuestionState(AppDbContext context)
        { 
            _context = context;
        }





        public override async Task<StateResult> ExecuteAsync(UserMessage message)
        {
            return Step switch
            {
                0 => await ShowAdminSelection(),
                1 => await ProcessAdminSelection(message),
                2 => await ProcessQuestion(message),
                _ => new StateResult("Ошибка", StateAction.End)
            };
        }

        private async Task<StateResult> ShowAdminSelection()
        {
            Step = 1;
            
            var admins = await _context.Users
                .Where(u => u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked)
                .ToListAsync();
                
            if (!admins.Any())
            {
                return new StateResult("❌ Администраторы недоступны", StateAction.End);
            }
            
            var keyboard = VkKeyboard.Create(false, true);
            foreach (var admin in admins)
            {
                keyboard.AddRow();
                keyboard.AddButton(admin.FullName, VkButtonColor.Primary);
            }
            
            return new StateResult("👥 Выберите администратора:", StateAction.Stay, keyboard: keyboard);
        }
        
        private async Task<StateResult> ProcessAdminSelection(UserMessage message)
        {
            var adminName = message.Text?.Trim();
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.FullName == adminName && u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked);
                
            if (admin == null)
            {
                return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
            }
            
            _selectedAdminId = admin.VkUserId;
            Step = 2;
            return new StateResult($"❓ Напишите вопрос для {admin.FullName}:", StateAction.Stay);
        }

        private async Task<StateResult> ProcessQuestion(UserMessage message)
        {
            var messageText = message.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                return new StateResult("❌ Вопрос не может быть пустым\n❓ Напишите ваш вопрос:", StateAction.Stay);
            }

            var msg = new Message
            {
                SenderId = message.UserId,
                Payload = $"{{\"type\":\"{PayloadType.Question}\",\"text\":\"{messageText.Replace("\"", "\\\"")}\"}}"
            };
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            _context.MessageDeliveries.Add(new MessageDelivery
            {
                MessageId = msg.Id,
                RecipientId = _selectedAdminId,
                DeliveryStatus = MessageStatus.Pending.ToString(),
                DispatchTime = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var admin = await _context.Users.FirstAsync(u => u.VkUserId == _selectedAdminId);
            return new StateResult($"✅ Вопрос отправлен администратору {admin.FullName}", StateAction.End);
        }
    }
}

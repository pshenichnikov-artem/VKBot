using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States.UserState
{
    [State("вопрос", UserRole.Student)]
    [Description(0, "❓ Обращение к администрации\nКоманда для отправки вопросов администраторам. Опишите вашу проблему или вопрос, и администраторы ответят вам.")]
    [Description(1, "📝 Напишите вопрос\nОпишите вашу проблему или вопрос подробно")]
    public class QuestionState : BaseState
    {
        [NonSerialized]
        private readonly AppDbContext _context;

        public QuestionState(AppDbContext context)
        { 
            _context = context;
        }





        public override async Task<StateResult> ExecuteAsync(UserMessage message)
        {
            return Step switch
            {
                0 => AskQuestion(),
                1 => await ProcessQuestion(message),
                _ => new StateResult("Ошибка", StateAction.End)
            };
        }

        private StateResult AskQuestion()
        {
            Step = 1;
            return new StateResult("❓ Введите ваш вопрос:", StateAction.Stay);
        }

        private async Task<StateResult> ProcessQuestion(UserMessage message)
        {
            var messageText = message.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                return new StateResult("❌ Вопрос не может быть пустым\n❓ Напишите ваш вопрос:", StateAction.Stay);
            }

            var admins = await _context.Users
                .Where(u => u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked)
                .ToListAsync();

            var msg = new Message
            {
                SenderId = message.UserId,
                Payload = $"{{\"type\":\"{PayloadType.Question}\",\"text\":\"{messageText.Replace("\"", "\\\"")}\"}}"
            };
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            foreach (var admin in admins)
            {
                _context.MessageDeliveries.Add(new MessageDelivery
                {
                    MessageId = msg.Id,
                    RecipientId = admin.VkUserId,
                    DeliveryStatus = MessageStatus.Pending.ToString(),
                    DispatchTime = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            return new StateResult($"✅ Вопрос отправлен\n👥 Администраторов: {admins.Count}", StateAction.End);
        }
    }
}

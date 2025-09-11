using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Application.States.UserState
{
    public class QuestionState : BaseState
    {
        public QuestionState(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override string Description => _step switch
        {
            0 => "❓ Обращение к администрации\nКоманда для отправки вопросов администраторам. Опишите вашу проблему или вопрос, и администраторы ответят вам.",
            1 => "📝 Напишите вопрос\nОпишите вашу проблему или вопрос подробно",
            _ => "❌ Ошибка в процессе отправки вопроса"
        };

        public override bool IsEntryPoint => true;
        public override string? Command => "/question";
        public override UserRole[] AllowedRoles => new[] { UserRole.Student };

        protected override Dictionary<int, Type[]> AvailableStates => new();

        public override async Task<StateResult> ExecuteAsync(UserMessage message)
        {
            return _step switch
            {
                0 => AskQuestion(),
                1 => await ProcessQuestion(message),
                _ => StateResult.Success("Ошибка", StateAction.End)
            };
        }

        private StateResult AskQuestion()
        {
            _step = 1;
            return StateResult.Success("❓ Введите ваш вопрос:", StateAction.Stay);
        }

        private async Task<StateResult> ProcessQuestion(UserMessage message)
        {
            var messageText = message.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                return StateResult.Success("❌ Вопрос не может быть пустым\n❓ Напишите ваш вопрос:", StateAction.Stay);
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var admins = await context.Users
                .Where(u => u.Role == UserRole.Admin.ToString() && u.IsConfirmed && !u.IsBlocked)
                .ToListAsync();

            var msg = new Message
            {
                SenderId = message.UserId,
                Payload = $"{{\"type\":\"{PayloadType.Question}\",\"text\":\"{messageText.Replace("\"", "\\\"")}\"}}"
            };
            context.Messages.Add(msg);
            await context.SaveChangesAsync();

            foreach (var admin in admins)
            {
                context.MessageDeliveries.Add(new MessageDelivery
                {
                    MessageId = msg.Id,
                    RecipientId = admin.VkUserId,
                    DeliveryStatus = MessageStatus.Pending.ToString(),
                    DispatchTime = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            return StateResult.Success($"✅ Вопрос отправлен\n👥 Администраторов: {admins.Count}", StateAction.End);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class MessageDelivery

        //Лолита
        //Ивент 101


        //Иван
        //Пришел ивент 102
        //Ответ ивана 103
        //Неотвеченные сообщения /events
        // Список ивенто
        //Продолжить Ответить
        //Пришел ивент 105
    {
        [Key]
        public long Id { get; set; }
        public long? MessageId { get; set; } // Сначала такой же, как и ParentMessageId, затем обновляется
        public Message? Message { get; set; } = null;

        public long RecipientId { get; set; }
        public User Recipient { get; set; } = null;

        public short RetryCount { get; set; } = 0;
        public DateTime? DispatchTime { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public DateTime? LastReminderAt { get; set; }
        [Required]
        public string DeliveryStatus { get; set; } = "pending";
        public bool isRead { get; set; }
    }
}

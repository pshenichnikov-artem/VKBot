using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Message
    {
        [Key]
        public long Id { get; set; }
        public long? SenderId { get; set; } = null;
        public User? Sender { get; set; } = null;
        public long? ReplyToMessageId { get; set; }
        public string? Payload { get; set; } //Json с доп параметрами
        public bool EnableReminder { get; set; } = false;
        public DateTime? LastReminderSent { get; set; }

        public ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    }
}
using System.ComponentModel.DataAnnotations;
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class MessageDelivery
    {
        [Key]
        public long Id { get; set; }
        public long? MessageId { get; set; }
        public Message? Message { get; set; } = null;

        public long RecipientId { get; set; }
        public User Recipient { get; set; } = null;

        public short RetryCount { get; set; } = 0;
        public DateTime? DispatchTime { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public DateTime? LastReminderAt { get; set; }
        [Required]
        public string DeliveryStatus { get; set; } = MessageStatus.Pending.ToString().ToLower();
        public bool isRead { get; set; }
    }
}

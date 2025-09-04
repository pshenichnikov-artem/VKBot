using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class MessageDelivery
    {
        [Key]
        public long MessageId { get; set; }
        public Message? Message { get; set; } = null;
        [Key]
        public long UserId { get; set; }
        public User? User { get; set; } = null;

        // поля очереди
        public short RetryCount { get; set; } = 0;
        public DateTime? BlockedUntil { get; set; }
        [Required]
        public string DeliveryStatus { get; set; }
    }
}

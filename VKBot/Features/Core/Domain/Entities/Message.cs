using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Message
    {
        [Key]
        public long Id { get; set; } //VkMessageId
        [Required]
        public string MessageType { get; set; } //Событие или директ
        public long? SenderId { get; set; } = null;
        public User? Sender { get; set; } = null;
        public long? RecipientId { get; set; } = null;
        public User? Recipient { get; set; } = null;
        public string? TagerGroup { get; set; } = null; //All Group
        public ICollection<MessageGroup> TargetList { get; set; } = new List<MessageGroup>();
        public ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    }
}

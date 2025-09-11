using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class User
    {
        [Key]
        public long VkUserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Role { get; set; }
        [Required]
        public bool IsConfirmed { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public long? GroupId { get; set; }
        public Group? Group { get; set; } = null;
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public ICollection<Message> RereceivedMessages { get; set; } = new List<Message>();
        public ICollection<MessageDelivery> MessageDeliveries { get; set; } = new List<MessageDelivery>();
    }
}

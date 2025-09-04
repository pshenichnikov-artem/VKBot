using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class Message
    {
        [Key]
        public long Id { get; set; }

        public long SenderId { get; set; }   // FK → User (Admin или User)
        public User Sender { get; set; } = null!;

        public RecipientType RecipientType { get; set; } //All / Group(потоку на уровне бизнеслогики)

        public long? TargetUserId { get; set; } // если личное сообщение
        public User? TargetUser { get; set; }

        public ICollection<MessageGroup> TargetGroups { get; set; } = new List<MessageGroup>();
    }

    public class MessageGroup
    {
        public long MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public long GroupId { get; set; }
        public Group Group { get; set; } = null!;
}

    public class MessageDelivery
    {
        [Key]
        public long MessageId { get; set; }
        public Message Message { get; set; } = null!;
        [Key]
        public long UserId { get; set; }
        public User User { get; set; } = null!;

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        // поля очереди
        public short RetryCount { get; set; } = 0;
        public DateTime? BlockedUntil { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsFailed { get; set; } = false;
    }
    public class Reply
    {
        [Key]
        public long Id { get; set; }

        public long MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public long SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

}

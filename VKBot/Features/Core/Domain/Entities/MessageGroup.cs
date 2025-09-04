using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class MessageGroup
    {
        [Key]
        public long MessageId { get; set; }
        public Message Message { get; set; } = null!;
        [Key]
        public long GroupId { get; set; }
        public Group Group { get; set; } = null!;
    }
}

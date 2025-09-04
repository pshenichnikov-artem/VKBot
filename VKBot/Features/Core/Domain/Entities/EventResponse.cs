using System.ComponentModel.DataAnnotations;

namespace VKBot.Features.Core.Domain.Entities
{
    public class EventResponse
    {
        [Key]
        public long MessageId { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; } = null;
        public long EventId { get; set; }
        public Event? Event { get; set; } = null;
    }
}
using VKBot.Features.Core.Domain.Enums;

namespace VKBot.Features.Core.Domain.Entities
{
    public class EventSend
    {
        public long EventId { get; set; }
        public Event? Event { get; set; } = null;
        public long UserId { get; set; }
        public User? User { get; set; } = null;
        public bool isRead { get; set; } = false;
        public string Status { get; set; } = EventSendStatus.Pending.ToString().ToLower();
    }
}

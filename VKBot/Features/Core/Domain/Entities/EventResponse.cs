namespace VKBot.Features.Core.Domain.Entities
{
    public class EventResponse
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; } = null;
        public long EventId { get; set; }
        public Event? Event { get; set; } = null;
        public long MessageId {  get; set; }
    }
}

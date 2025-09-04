namespace VKBot.Features.Core.Domain.Entities
{
    public class Inbox
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; } = null;
        public long MessageId { get; set; }
        public short CountRetry { get; set; } = 0;
    }
}

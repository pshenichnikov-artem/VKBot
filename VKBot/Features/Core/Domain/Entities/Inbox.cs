namespace VKBot.Features.Core.Domain.Entities
{
    public class Inbox
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public User? User { get; set; } = null;
        public long MessageId { get; set; }//Достать из вк и отправить
        public short CountRetry { get; set; } = 0;
        public DateTime? BlockTo { get; set; } = null;
    }
}

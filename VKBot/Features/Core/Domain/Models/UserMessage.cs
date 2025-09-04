namespace VKBot.Features.Core.Domain.Models
{
    public class UserMessage
    {
        public string Text { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long PeerId { get; set; }
        public long MessageId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long? ReplyToMessageId { get; set; }
        public List<MessageAttachment> Attachments { get; set; } = new();
    }

    public class MessageAttachment
    {
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long? OwnerId {  get; set; }
        public long? MediaId { get; set; }
    }
}
using System.Text.Json;

namespace VKBot.Features.VK.Models;

public class VkMessage
{
    public long UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public long? ReplyToMessageId { get; set; }
    public List<VkAttachment> Attachments { get; set; } = new();
}

//public class VkAttachment
//{
//    public string Type { get; set; } = string.Empty;
//    public string Url { get; set; } = string.Empty;
//    public string? FileName { get; set; }
//}
public class VkAttachment
{
    public string Type { get; set; }
    public JsonElement Payload {  get; set; }
}
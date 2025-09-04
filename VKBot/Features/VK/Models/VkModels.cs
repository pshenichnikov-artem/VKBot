using System.Text.Json;
using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models;

public class VkMessagesResponse
{
    [JsonPropertyName("response")]
    public VkMessagesData Response { get; set; } = new();
}

public class VkMessagesData
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("items")]
    public List<VkMessageItem> Items { get; set; } = new();
}

public class VkMessageItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("from_id")]
    public long FromId { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("attachments")]
    public List<VkAttachmentItem> Attachments { get; set; } = new();
    
    [JsonPropertyName("reply_message")]
    public VkMessageItem? ReplyMessage { get; set; }
}

public class VkAttachmentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("photo")]
    public VkPhoto? Photo { get; set; }
    
    [JsonPropertyName("doc")]
    public VkDoc? Doc { get; set; }
    
    [JsonPropertyName("audio")]
    public VkAudio? Audio { get; set; }
    
    [JsonPropertyName("video")]
    public VkVideo? Video { get; set; }
}

public class VkPhoto
{
    [JsonPropertyName("sizes")]
    public List<VkPhotoSize> Sizes { get; set; } = new();
}

public class VkPhotoSize
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class VkDoc
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("ext")]
    public string Extension { get; set; } = string.Empty;
}

public class VkAudio
{
    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}

public class VkVideo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
    
    [JsonPropertyName("player")]
    public string Player { get; set; } = string.Empty;
}

public class VkLongPollServerResponse
{
    [JsonPropertyName("response")]
    public LongPollServer Response { get; set; } = new();
}
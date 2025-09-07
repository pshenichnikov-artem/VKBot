using System.Text.Json;
using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Domain.Models;

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
    
    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }
    
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
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }
    
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
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("ext")]
    public string Extension { get; set; } = string.Empty;
}

public class VkAudio
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }
    
    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}

public class VkVideo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }
    
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


public class VkSendMessageResponse
{
    [JsonPropertyName("response")]
    public long MessageId { get; set; }
}

public class VkErrorResponse
{
    [JsonPropertyName("error")]
    public VkError Error { get; set; }
}

public class VkError
{
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string ErrorMessage { get; set; }
}
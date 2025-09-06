using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Domain.Models;

public class VkUploadServerResponse
{
    [JsonPropertyName("response")]
    public VkUploadServer Response { get; set; } = new();
}

public class VkUploadServer
{
    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; } = string.Empty;
}

public class VkDocUploadResponse
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}

public class VkDocSaveResponse
{
    [JsonPropertyName("response")]
    public VkDocSaveData Response { get; set; } = new();
}

public class VkDocSaveData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("doc")]
    public VkDoc Doc { get; set; } = new();
}

public class VkPhotoUploadResponse
{
    [JsonPropertyName("server")]
    public int Server { get; set; }
    
    [JsonPropertyName("photo")]
    public string Photo { get; set; } = string.Empty;
    
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
}

public class VkPhotoSaveResponse
{
    [JsonPropertyName("response")]
    public List<VkPhoto> Response { get; set; } = new();
}
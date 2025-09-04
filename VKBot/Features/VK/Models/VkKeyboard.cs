using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models;

public class VkKeyboard
{
    [JsonPropertyName("one_time")]
    public bool OneTime { get; set; } = false;
    
    [JsonPropertyName("buttons")]
    public List<List<VkButton>> Buttons { get; set; } = new();
    [JsonPropertyName("inline")]
    public bool Inline {  get; set; } = false;
}

public class VkButton
{
    [JsonPropertyName("action")]
    public VkButtonAction Action { get; set; } = new();
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = "secondary";
}

public class VkButtonAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}
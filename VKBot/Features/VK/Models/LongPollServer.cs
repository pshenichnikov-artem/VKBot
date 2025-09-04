using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models;

public class LongPollServer
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;
    [JsonPropertyName("ts")]
    public long Ts { get; set; } = default;
}
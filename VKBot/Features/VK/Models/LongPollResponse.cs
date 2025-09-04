using System.Text.Json;
using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models;

internal class LongPollResponse
{
    [JsonPropertyName("ts")]
    public long Ts { get; set; } = default;
    [JsonPropertyName("updates")]
    public JsonElement Updates { get; set; }
    [JsonPropertyName("failed")]
    public long Failed { get; internal set; }
}
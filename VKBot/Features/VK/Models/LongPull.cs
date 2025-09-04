using System.Text.Json;
using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models
{
    public class LongPollServer
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("server")]
        public string Server { get; set; } = string.Empty;

        [JsonPropertyName("ts")]
        public long Ts { get; set; }
    }

    public class LongPollResponse
    {
        [JsonPropertyName("ts")]
        public long Ts { get; set; }

        [JsonPropertyName("updates")]
        public JsonElement Updates { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }
    }
}

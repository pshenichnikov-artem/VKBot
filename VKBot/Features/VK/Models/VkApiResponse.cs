using System.Text.Json.Serialization;

namespace VKBot.Features.VK.Models;

internal class VkApiResponse<T>
{
    [JsonPropertyName("response")]
    public T Response { get; set; }
}
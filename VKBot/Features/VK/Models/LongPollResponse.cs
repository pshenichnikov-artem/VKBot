namespace VKBot.Features.VK.Models;

internal class LongPollResponse
{
    public string Ts { get; set; } = string.Empty;
    public VkUpdate[]? Updates { get; set; }
}
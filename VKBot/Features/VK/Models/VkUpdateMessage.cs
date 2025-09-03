namespace VKBot.Features.VK.Models;

internal class VkUpdateMessage
{
    public long FromId { get; set; }
    public string Text { get; set; } = string.Empty;
}
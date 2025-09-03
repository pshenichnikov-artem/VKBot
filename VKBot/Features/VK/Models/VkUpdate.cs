namespace VKBot.Features.VK.Models;

internal class VkUpdate
{
    public string Type { get; set; } = string.Empty;
    public VkUpdateObject Object { get; set; } = new();
}
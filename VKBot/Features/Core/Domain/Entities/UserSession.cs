namespace VKBot.Features.Core.Domain.Entities;

public class UserSession
{
    public long VkUserId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public int Step { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}
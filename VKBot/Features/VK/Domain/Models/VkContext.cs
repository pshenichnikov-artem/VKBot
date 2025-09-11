using System.Text.Json;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Application.States;
using VKBot.Features.Core.Domain.Models;

namespace VKBot.Features.VK.Domain.Models;

public class VkContext
{
    public JsonElement Update { get; set; }
    public VkMessageItem? Message { get; set; }
    public User? User { get; set; }
    public BaseState? FoundState { get; set; }
    public List<VkResult> Results { get; set; } = new();
}

public class VkResult
{
    public string? Text { get; set; }
    public long? UserId { get; set; }
    public VkKeyboard? Keyboard { get; set; }
    public bool ForwardMessage { get; set; }
    public List<StateAttachment> Attachments { get; set; } = new();
}
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.Core.Domain.Models;

public class StateResult
{
    public string? Text { get; set; }
    public List<StateAttachment> Attachments { get; set; } = new();
    public long? ReplyToMessageId { get; set; }
    public VkKeyboard? Keyboard { get; set; }
    public StateAction Action { get; set; } = StateAction.End;
    public Type? NextStateType { get; set; }
    
    public StateResult(string? text = null, StateAction action = StateAction.End, Type? nextStateType = null, long? replyToMessageId = null, List<StateAttachment>? attachments = null, VkKeyboard? keyboard = null)
    {
        Text = text;
        Action = action;
        NextStateType = nextStateType;
        ReplyToMessageId = replyToMessageId;
        Attachments = attachments ?? new List<StateAttachment>();
        Keyboard = keyboard;
    }
}

public class StateAttachment
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long? OwnerId { get; set; }
    public long? MediaId { get; set; }
}
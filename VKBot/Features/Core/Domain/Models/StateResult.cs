using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.Core.Domain.Models;

public class StateResult
{
    public List<StateMessage> Messages { get; set; } = new();
    public StateAction Action { get; set; } = StateAction.End;
    public Type? NextStateType { get; set; }
    
    public StateResult(string? text = null, StateAction action = StateAction.End, bool isForwardMessage = false, Type? nextStateType = null, long? replyToMessageId = null, List<StateAttachment>? attachments = null, VkKeyboard? keyboard = null, long forwardMessageId = 0)
    {
        Action = action;
        NextStateType = nextStateType;
        
        Messages.Add(new StateMessage
        {
            Text = text,
            Attachments = attachments ?? new List<StateAttachment>(),
            ReplyToMessageId = replyToMessageId,
            Keyboard = keyboard,
            IsForwardMessage = isForwardMessage,
            ForwardMessageId = forwardMessageId
        });
    }
    
    public StateResult(List<StateMessage> messages, StateAction action = StateAction.End, Type? nextStateType = null)
    {
        Messages = messages;
        Action = action;
        NextStateType = nextStateType;
    }
}

public class StateMessage
{
    public string? Text { get; set; }
    public List<StateAttachment> Attachments { get; set; } = new();
    public long? ReplyToMessageId { get; set; }
    public VkKeyboard? Keyboard { get; set; }

    public bool IsForwardMessage { get; set; }
    public long ForwardMessageId { get; set; }
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
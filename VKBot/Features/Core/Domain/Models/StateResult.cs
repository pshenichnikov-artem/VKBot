using VKBot.Features.VK.Models;

namespace VKBot.Features.Core.Domain.Models;

public class StateResult
{
    public string? Text { get; set; }
    public List<StateAttachment> Attachments { get; set; } = new();
    public long? ReplyToMessageId { get; set; }
    public VkKeyboard? Keyboard { get; set; }
    public bool IsSuccess { get; set; } = true;
    
    public static StateResult Success(string? text = null, long? replyToMessageId = null, List<StateAttachment>? attachments = null, VkKeyboard? keyboard = null)
    {
        return new StateResult
        {
            Text = text,
            ReplyToMessageId = replyToMessageId,
            Attachments = attachments ?? new List<StateAttachment>(),
            Keyboard = keyboard,
            IsSuccess = true
        };
    }
    
    public static StateResult Failure(string? errorText = null)
    {
        return new StateResult
        {
            Text = errorText,
            IsSuccess = false
        };
    }
}

public class StateAttachment
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? OwnerId { get; set; }
    public long? MediaId { get; set; }
}
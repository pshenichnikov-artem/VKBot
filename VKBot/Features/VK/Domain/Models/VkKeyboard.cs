using System.Text.Json.Serialization;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.VK.Domain.Models;

public class VkKeyboard
{
    [JsonPropertyName("one_time")]
    public bool OneTime { get; set; } = false;

    [JsonPropertyName("buttons")]
    public List<List<VkButton>> Buttons { get; set; } = new();
    [JsonPropertyName("inline")]
    public bool Inline {  get; set; } = false;

    public static VkKeyboard Create(bool oneTime = false, bool inline = false)
    {
        return new VkKeyboard
        {
            OneTime = oneTime,
            Inline = inline,
            Buttons = new()
        };
    }

    public void AddRow()
    {
        Buttons.Add(new List<VkButton>());
    }

    public void AddButton(string label, VkButtonColor color = VkButtonColor.Secondary, int rowIndex = -1, string type = "text", string? payload = null)
    {
        if(Buttons.Count == 0 || rowIndex >= Buttons.Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        var targetRow = rowIndex >= 0 && rowIndex < Buttons.Count
            ? Buttons[rowIndex]
            : Buttons[^1];

        targetRow.Add(new VkButton
        {
            Color = color.ToString().ToLower(),
            Action = new VkButtonAction
            {
                Type = type,
                Payload = payload,
                Label = label
            }
        });

        return;
    }
}

public class VkButton
{
    [JsonPropertyName("action")]
    public VkButtonAction Action { get; set; } = new();
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = "secondary";
}

public class VkButtonAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}
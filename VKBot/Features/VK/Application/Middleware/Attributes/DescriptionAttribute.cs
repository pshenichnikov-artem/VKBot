namespace VKBot.Features.VK.Application.Middleware.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DescriptionAttribute : Attribute
{
    public int Step { get; }
    public string Text { get; }

    public DescriptionAttribute(int step, string text)
    {
        Step = step;
        Text = text;
    }
}
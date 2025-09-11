namespace VKBot.Features.VK.Application.Middleware.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TransitionAttribute : Attribute
{
    public int Step { get; }
    public Type[] AvailableStates { get; }

    public TransitionAttribute(int step, params Type[] availableStates)
    {
        Step = step;
        AvailableStates = availableStates;
    }
}
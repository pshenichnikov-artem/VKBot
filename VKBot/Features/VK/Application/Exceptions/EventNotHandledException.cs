namespace VKBot.Features.VK.Application.Exceptions;

public class EventNotHandledException : Exception
{
    public EventNotHandledException() : base("Событие не обрабатывается")
    {
    }
}
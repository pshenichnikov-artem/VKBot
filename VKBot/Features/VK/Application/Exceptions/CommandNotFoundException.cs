namespace VKBot.Features.VK.Application.Exceptions;

public class CommandNotFoundException : Exception
{
    public CommandNotFoundException() : base("Неизвестная команда") { }
}
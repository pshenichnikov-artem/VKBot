namespace VKBot.Features.VK.Application.Exceptions;

public class UserBlockedException : Exception
{
    public UserBlockedException() : base("Ваш аккаунт заблокирован") { }
}
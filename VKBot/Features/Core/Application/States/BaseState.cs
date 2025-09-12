using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[Serializable]
public abstract class BaseState
{
    public int Step { get; protected set; } = 0;

    public void ReInject(IServiceProvider serviceProvider)
    {
        var allFields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var unmarkedServiceFields = new List<FieldInfo>();
        
        foreach (var field in allFields)
        {
            var service = serviceProvider.GetService(field.FieldType);
            if (service != null)
            {
                field.SetValue(this, service);
                
                if (field.GetCustomAttribute<NonSerializedAttribute>() == null)
                {
                    unmarkedServiceFields.Add(field);
                }
            }
        }
        
        if (unmarkedServiceFields.Any())
        {
            throw new InvalidOperationException($"Поля сервисов должны быть помечены [NonSerialized]: {string.Join(", ", unmarkedServiceFields.Select(f => f.Name))}");
        }
    }

    public abstract Task<StateResult> ExecuteAsync(UserMessage message);

    public static string GetCommand<T>() where T : BaseState
    {
        var command = typeof(T).GetCustomAttribute<StateAttribute>()?.Command;
        if (string.IsNullOrEmpty(command))
            throw new InvalidOperationException($"У состояния {typeof(T).Name} нет команды");
        return command;
    }
}

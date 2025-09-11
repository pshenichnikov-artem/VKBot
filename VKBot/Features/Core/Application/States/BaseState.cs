using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace VKBot.Features.Core.Application.States;

[Serializable]
public abstract class BaseState
{
    public int Step { get; protected set; } = 0;

    public void ReInject(IServiceProvider serviceProvider)
    {
        var allFields = GetType().GetFields();
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
}

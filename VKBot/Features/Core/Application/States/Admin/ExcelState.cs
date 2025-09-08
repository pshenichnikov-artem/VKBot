using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.Core.Application.Interfaces;
using System.Text.Json;

namespace VKBot.Features.Core.Application.States;

public class ExcelState : BaseState
{
    public ExcelState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => "Генерация Excel файла\nСоздает и отправляет Excel-файл со списком всех подтвержденных пользователей с информацией о группах и контактных данных";
    public override bool IsEntryPoint => true;
    public override string? Command => "Excel";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        if (message.Payload == null || !message.Payload.TryGetValue("messageId", out var messageIdElement))
        {
            return StateResult.Success("Недоступная функция", StateAction.End);
        }
        
        long eventMessageId;
        try
        {
            eventMessageId = Convert.ToInt64(messageIdElement);
        }
        catch
        {
            return StateResult.Success("Ошибка обработки кнопки", StateAction.End);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vkBot = scope.ServiceProvider.GetRequiredService<IVkBot>();

        var eventMsg = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == eventMessageId && m.Payload != null);

        if (eventMsg == null)
        {
            return StateResult.Success("Сообщение не найдено", StateAction.End);
        }

        var eventPayload = JsonSerializer.Deserialize<JsonElement>(eventMsg.Payload!);
        var messageType = eventPayload.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "unknown";
        
        var providers = scope.ServiceProvider.GetServices<IExcelReportProvider>();
        var provider = providers.FirstOrDefault(p => p.GetMessageType() == messageType);
        
        if (provider == null)
        {
            return StateResult.Success("Неподдерживаемый тип сообщения", StateAction.End);
        }
        
        var deliveries = await context.MessageDeliveries
            .Include(md => md.Recipient)
            .ThenInclude(u => u.Group)
            .Where(md => md.MessageId == eventMessageId)
            .ToListAsync();
            
        var responses = await context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ReplyToMessageId == eventMessageId && m.Payload != null)
            .ToListAsync();
            
        var excelBytes = await provider.GenerateReport(eventMsg, deliveries, responses);
        var fileName = provider.GetFileName();
        
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "reports", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, excelBytes);
        
        var attachment = new StateAttachment
        {
            Type = "doc",
            FilePath = filePath,
            FileName = fileName
        };
        
        return StateResult.Success(provider.GetReportTitle(), StateAction.End, attachments: new List<StateAttachment> { attachment });
    }


}
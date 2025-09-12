using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Interfaces;
using VKBot.Features.Core.Application.Interfaces;
using System.Text.Json;
using VKBot.Features.VK.Application.Middleware.Attributes;

namespace VKBot.Features.Core.Application.States;

[State(PayloadType.Excel, UserRole.Admin)]
[Description(0, "Генерация Excel файла")]
public class ExcelState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly IVkBot _vkBot;
    [NonSerialized]
    private readonly IReadOnlyCollection<IExcelReportProvider> _excelProvider;
    [NonSerialized]
    private readonly ILogger<ExcelState> _logger;

    public ExcelState(AppDbContext context, IVkBot vkBot, IEnumerable<IExcelReportProvider> excelProvider, ILogger<ExcelState> logger)
    { 
        _context = context;
        _vkBot = vkBot;
        _excelProvider = excelProvider.ToList();
        _logger = logger;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        _logger.LogInformation("ExcelState Payload: {Payload}", string.Join(", ", message.Payload?.Select(kvp => $"{kvp.Key}={kvp.Value}") ?? new string[0]));
        
        if (message.Payload == null || !message.Payload.TryGetValue("messageId", out var messageIdElement))
        {
            return new StateResult("Недоступная функция", StateAction.End);
        }
        
        long eventMessageId;
        try
        {
            eventMessageId = Convert.ToInt64(messageIdElement.ToString());
        }
        catch
        {
            return new StateResult("Ошибка обработки кнопки", StateAction.End);
        }

        var eventMsg = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == eventMessageId && m.Payload != null);

        if (eventMsg == null)
        {
            return new StateResult("Сообщение не найдено", StateAction.End);
        }

        var eventPayload = JsonSerializer.Deserialize<JsonElement>(eventMsg.Payload!);
        var messageType = eventPayload.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "unknown";
        
        var provider = _excelProvider.FirstOrDefault(p => p.GetMessageType() == messageType);
        
        if (provider == null)
        {
            return new StateResult("Неподдерживаемый тип сообщения", StateAction.End);
        }
        
        var deliveries = await _context.MessageDeliveries
            .Include(md => md.Recipient)
            .ThenInclude(u => u.Group)
            .Where(md => md.MessageId == eventMessageId)
            .IgnoreQueryFilters()
            .ToListAsync();
            
        var responses = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ReplyToMessageId == eventMessageId && m.Payload != null)
            .IgnoreQueryFilters()
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
        
        return new StateResult(provider.GetReportTitle(), StateAction.End, attachments: new List<StateAttachment> { attachment });
    }


}

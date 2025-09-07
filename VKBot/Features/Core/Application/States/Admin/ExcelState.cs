using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Application.Interfaces;
using System.Text.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
        // Проверяем payload на наличие messageId из кнопки Excel
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
        var eventTitle = eventPayload.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : "Без заголовка";

        var deliveries = await context.MessageDeliveries
            .Include(md => md.Recipient)
            .ThenInclude(u => u.Group)
            .Where(md => md.MessageId == eventMessageId)
            .ToListAsync();

        var responses = await context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ReplyToMessageId == eventMessageId && m.Payload != null && m.Payload.Contains("\"type\":\"event_response\""))
            .ToListAsync();

        var excelBytes = await CreateExcelReport(eventTitle, deliveries, responses);
        
        var fileName = $"event_report_{eventMessageId}_{DateTime.UtcNow.AddHours(3):yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "reports", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, excelBytes);
        
        var attachment = new StateAttachment
        {
            Type = "doc",
            FilePath = filePath,
            FileName = fileName
        };
        
        return StateResult.Success($"Отчет по событию '{eventTitle}'", StateAction.End, attachments: new List<StateAttachment> { attachment });
    }

    private async Task<byte[]> CreateExcelReport(string eventTitle, List<MessageDelivery> deliveries, List<Message> responses)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Отчет по событию");
        
        // Заголовок
        worksheet.Cells[1, 1].Value = $"Отчет по событию: {eventTitle}";
        worksheet.Cells[1, 1, 1, 4].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;
        
        // Шапка таблицы
        worksheet.Cells[3, 1].Value = "Группа";
        worksheet.Cells[3, 2].Value = "Студент";
        worksheet.Cells[3, 3].Value = "Статус";
        worksheet.Cells[3, 4].Value = "Ответ";
        worksheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;
        
        var row = 4;
        var respondedCount = 0;
        var readCount = 0;
        var unreadCount = 0;
        
        var groupedDeliveries = deliveries.GroupBy(d => d.Recipient?.Group?.Name ?? "Нет группы").OrderBy(g => g.Key);
        
        foreach (var group in groupedDeliveries)
        {
            foreach (var delivery in group.OrderBy(d => d.Recipient?.FullName))
            {
                var response = responses.FirstOrDefault(r => r.SenderId == delivery.RecipientId);
                var status = response != null ? "Ответил" : 
                            delivery.isRead ? "Прочитано" : "Не прочитано";
                
                if (response != null) respondedCount++;
                else if (delivery.isRead) readCount++;
                else unreadCount++;
                
                var responseText = "";
                if (response != null)
                {
                    var responsePayload = JsonSerializer.Deserialize<JsonElement>(response.Payload!);
                    responseText = responsePayload.GetProperty("text").GetString() ?? "";
                }
                
                worksheet.Cells[row, 1].Value = group.Key;
                worksheet.Cells[row, 2].Value = delivery.Recipient?.FullName ?? "Неизвестно";
                worksheet.Cells[row, 3].Value = status;
                worksheet.Cells[row, 4].Value = responseText;
                
                row++;
            }
        }
        
        // Итоги
        row += 2;
        worksheet.Cells[row, 1].Value = "Итоги:";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        row++;
        worksheet.Cells[row, 1].Value = "Ответили:";
        worksheet.Cells[row, 2].Value = respondedCount;
        row++;
        worksheet.Cells[row, 1].Value = "Прочитали:";
        worksheet.Cells[row, 2].Value = readCount;
        row++;
        worksheet.Cells[row, 1].Value = "Не прочитали:";
        worksheet.Cells[row, 2].Value = unreadCount;
        
        // Авторазмер колонок
        worksheet.Cells.AutoFitColumns();
        
        return await package.GetAsByteArrayAsync();
    }
}
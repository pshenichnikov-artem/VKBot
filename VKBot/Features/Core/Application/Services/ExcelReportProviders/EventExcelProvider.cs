using System.Text.Json;
using OfficeOpenXml;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Application.Services.ExcelReportProviders;

public class EventExcelProvider : IExcelReportProvider
{
    public string GetMessageType() => "event";
    public string GetFileName() => $"event_report_{DateTime.UtcNow.AddHours(3):yyyy_MM_dd_HH_mm}.xlsx";
    public string GetReportTitle() => "Отчет по событию";

    public async Task<byte[]> GenerateReport(Message message, List<MessageDelivery> deliveries, List<Message> responses)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload!);
        var eventTitle = payload.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : "Без заголовка";

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Отчет по событию");

        worksheet.Cells[1, 1].Value = $"Отчет по событию: {eventTitle}";
        worksheet.Cells[1, 1, 1, 4].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

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
                var status = response != null ? "Ответил" : delivery.isRead ? "Прочитано" : "Не прочитано";

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

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync();
    }
}
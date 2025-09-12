using System.Text.Json;
using OfficeOpenXml;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;


namespace VKBot.Features.Core.Application.Services.ExcelReportProviders;

public class AlertExcelProvider : IExcelReportProvider
{
    public string GetMessageType() => PayloadType.Alert.ToString();
    public string GetFileName() => $"alert_report_{DateTime.UtcNow.AddHours(3):yyyy_MM_dd_HH_mm}.xlsx";
    public string GetReportTitle() => $"Отчет по тревоге";

    public async Task<byte[]> GenerateReport(Message message, List<MessageDelivery> deliveries, List<Message> responses)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Отчет по тревоге");

        worksheet.Cells[1, 1].Value = $"Отчет по воздушной тревоге от {DateTime.UtcNow.AddHours(3):dd.MM.yyyy HH:mm} МСК";
        worksheet.Cells[1, 1, 1, 4].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

        worksheet.Cells[3, 1].Value = "Группа";
        worksheet.Cells[3, 2].Value = "Студент";
        worksheet.Cells[3, 3].Value = "Статус";
        worksheet.Cells[3, 4].Value = "Количество в укрытии";
        worksheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;

        var row = 4;
        var respondedCount = 0;
        var totalStudents = 0;

        var groupedDeliveries = deliveries.GroupBy(d => d.Recipient?.Group?.Name ?? "Нет группы").OrderBy(g => g.Key);

        foreach (var group in groupedDeliveries)
        {
            foreach (var delivery in group.OrderBy(d => d.Recipient?.FullName))
            {
                var response = responses.FirstOrDefault(r => r.SenderId == delivery.RecipientId);
                var status = response != null ? "Ответил" : "Не ответил";

                var count = "";
                if (response != null)
                {
                    respondedCount++;
                    var responsePayload = JsonSerializer.Deserialize<JsonElement>(response.Payload!);
                    var studentCount = responsePayload.GetProperty("count").GetInt32();
                    totalStudents += studentCount;
                    count = studentCount.ToString();
                }

                worksheet.Cells[row, 1].Value = group.Key;
                worksheet.Cells[row, 2].Value = delivery.Recipient?.FullName ?? "Неизвестно";
                worksheet.Cells[row, 3].Value = status;
                worksheet.Cells[row, 4].Value = count;
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
        worksheet.Cells[row, 1].Value = "Общее количество в укрытии:";
        worksheet.Cells[row, 2].Value = totalStudents;

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync();
    }
}
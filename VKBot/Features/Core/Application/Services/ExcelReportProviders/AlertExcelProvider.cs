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
        worksheet.Cells[1, 1, 1, 7].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 14;

        worksheet.Cells[3, 1].Value = "Группа";
        worksheet.Cells[3, 2].Value = "Студент";
        worksheet.Cells[3, 3].Value = "Статус";
        worksheet.Cells[3, 4].Value = "Университетская";
        worksheet.Cells[3, 5].Value = "Гоголя";
        worksheet.Cells[3, 6].Value = "Голландия";
        worksheet.Cells[3, 7].Value = "Всего";
        worksheet.Cells[3, 1, 3, 7].Style.Font.Bold = true;

        var row = 4;
        var respondedCount = 0;
        var totalUniversitetskaya = 0;
        var totalGogolya = 0;
        var totalGollandiya = 0;
        var totalStudents = 0;

        var groupedDeliveries = deliveries.GroupBy(d => d.Recipient?.Group?.Name ?? "Нет группы").OrderBy(g => g.Key);

        foreach (var group in groupedDeliveries)
        {
            var groupUniversitetskaya = 0;
            var groupGogolya = 0;
            var groupGollandiya = 0;
            var groupTotal = 0;
            
            foreach (var delivery in group.OrderBy(d => d.Recipient?.FullName))
            {
                var response = responses.FirstOrDefault(r => r.SenderId == delivery.RecipientId);
                var status = response != null ? "Ответил" : "Не ответил";

                var universitetskaya = "";
                var gogolya = "";
                var gollandiya = "";
                var total = "";
                
                if (response != null)
                {
                    respondedCount++;
                    var responsePayload = JsonSerializer.Deserialize<JsonElement>(response.Payload!);
                    
                    var univCount = responsePayload.TryGetProperty("universitetskaya", out var univProp) ? univProp.GetInt32() : 0;
                    var gogCount = responsePayload.TryGetProperty("gogolya", out var gogProp) ? gogProp.GetInt32() : 0;
                    var golCount = responsePayload.TryGetProperty("gollandiya", out var golProp) ? golProp.GetInt32() : 0;
                    var totCount = responsePayload.TryGetProperty("total", out var totProp) ? totProp.GetInt32() : (univCount + gogCount + golCount);
                    
                    universitetskaya = univCount.ToString();
                    gogolya = gogCount.ToString();
                    gollandiya = golCount.ToString();
                    total = totCount.ToString();
                    
                    groupUniversitetskaya += univCount;
                    groupGogolya += gogCount;
                    groupGollandiya += golCount;
                    groupTotal += totCount;
                }

                worksheet.Cells[row, 1].Value = group.Key;
                worksheet.Cells[row, 2].Value = delivery.Recipient?.FullName ?? "Неизвестно";
                worksheet.Cells[row, 3].Value = status;
                worksheet.Cells[row, 4].Value = universitetskaya;
                worksheet.Cells[row, 5].Value = gogolya;
                worksheet.Cells[row, 6].Value = gollandiya;
                worksheet.Cells[row, 7].Value = total;
                row++;
            }
            
            // Добавляем строку с итогом по группе
            worksheet.Cells[row, 1].Value = $"Итог по {group.Key}:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 4].Value = groupUniversitetskaya;
            worksheet.Cells[row, 5].Value = groupGogolya;
            worksheet.Cells[row, 6].Value = groupGollandiya;
            worksheet.Cells[row, 7].Value = groupTotal;
            worksheet.Cells[row, 4, row, 7].Style.Font.Bold = true;
            row += 2;
            
            totalUniversitetskaya += groupUniversitetskaya;
            totalGogolya += groupGogolya;
            totalGollandiya += groupGollandiya;
            totalStudents += groupTotal;
        }

        worksheet.Cells[row, 1].Value = "Общие итоги:";
        worksheet.Cells[row, 1].Style.Font.Bold = true;
        worksheet.Cells[row, 4].Value = totalUniversitetskaya;
        worksheet.Cells[row, 5].Value = totalGogolya;
        worksheet.Cells[row, 6].Value = totalGollandiya;
        worksheet.Cells[row, 7].Value = totalStudents;
        worksheet.Cells[row, 1, row, 7].Style.Font.Bold = true;
        row += 2;
        worksheet.Cells[row, 1].Value = "Ответили:";
        worksheet.Cells[row, 2].Value = respondedCount;

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync();
    }
}
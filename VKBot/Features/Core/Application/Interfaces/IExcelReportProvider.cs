using VKBot.Features.Core.Domain.Entities;

namespace VKBot.Features.Core.Application.Interfaces;

public interface IExcelReportProvider
{
    string GetMessageType();
    Task<byte[]> GenerateReport(Message message, List<MessageDelivery> deliveries, List<Message> responses);
    string GetFileName();
    string GetReportTitle();
}
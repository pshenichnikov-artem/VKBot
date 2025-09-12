using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.VK.Application.Exceptions;
using VKBot.Features.VK.Domain.Models;

namespace VKBot.Features.VK.Application.Middleware;

public class ParseMessageMiddleware : MiddlewareBase
{
    private const int NEW_MESSAGE_EVENT = 4;
    private const int OUTGOING_MESSAGE_FLAG = 2;
    private const int MIN_MESSAGE_FIELDS = 7;
    
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ParseMessageMiddleware(ILogger<ParseMessageMiddleware> logger, IConfiguration configuration, HttpClient httpClient) : base(logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public override async Task InvokeAsync(VkContext context, Func<Task> next)
    {
        if (context.Update.ValueKind != JsonValueKind.Array)
            throw new EventNotHandledException();

        var fields = context.Update.EnumerateArray().ToArray();
        
        if (fields.Length < MIN_MESSAGE_FIELDS)
            throw new EventNotHandledException();

        if (fields[0].GetInt32() != NEW_MESSAGE_EVENT)
            throw new EventNotHandledException();

        var flags = fields[2].GetInt32();
        if ((flags & OUTGOING_MESSAGE_FLAG) != 0)
            throw new EventNotHandledException();

        var userId = fields[3].GetInt64();
        var messageId = fields[1].GetInt64();
        
        context.Message = new VkMessageItem
        {
            Id = messageId,
            FromId = userId,
            PeerId = userId,
            Text = fields[6].GetString() ?? string.Empty,
            Attachments = new List<VkAttachmentItem>()
        };
        
        await EnrichWithAttachments(context.Message, messageId);
        _logger.LogInformation("Получено сообщение от {UserId}: {Text}", userId, context.Message.Text);
        await next();
    }
    
    private async Task EnrichWithAttachments(VkMessageItem message, long messageId)
    {
            var url = $"https://api.vk.com/method/messages.getById?message_ids={messageId}&access_token={_configuration["VK:AccessToken"]}&v=5.131";
            var response = await _httpClient.GetStringAsync(url);
            var apiResponse = JsonSerializer.Deserialize<VkMessagesResponse>(response);
            
            if (apiResponse?.Response?.Items?.FirstOrDefault() is var item && item != null)
            {
                message.Attachments = item.Attachments;
                message.ReplyMessage = item.ReplyMessage;
                
                // Парсим payload если это строка с JSON
                if (item.Payload.HasValue && item.Payload.Value.ValueKind == JsonValueKind.String)
                {
                    var payloadString = item.Payload.Value.GetString();
                    if (!string.IsNullOrEmpty(payloadString))
                    {
                        try
                        {
                            message.Payload = JsonDocument.Parse(payloadString).RootElement;
                        }
                        catch
                        {
                            message.Payload = item.Payload;
                        }
                    }
                }
                else
                {
                    message.Payload = item.Payload;
                }
            }
    }
}
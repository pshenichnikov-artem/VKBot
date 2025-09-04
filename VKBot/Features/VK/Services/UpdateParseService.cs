using System.Text.Json;
using VKBot.Features.VK.Models;

namespace VKBot.Features.VK.Services
{
    public class UpdateParseService
    {
        private const int NEW_MESSAGE_EVENT = 4;
        private const int OUTGOING_MESSAGE_FLAG = 2;
        private const int MIN_MESSAGE_FIELDS = 7;
        
        private const int EVENT_TYPE_INDEX = 0;
        private const int MESSAGE_ID_INDEX = 1;
        private const int FLAGS_INDEX = 2;
        private const int USER_ID_INDEX = 3;
        private const int TIMESTAMP_INDEX = 4;
        private const int SUBJECT_INDEX = 5;
        private const int TEXT_INDEX = 6;
        private const int ATTACHMENTS_INDEX = 7;
        
        private readonly ILogger<UpdateParseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        public UpdateParseService(ILogger<UpdateParseService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }
        
        public async Task<List<VkMessageItem>> GetNewMessages(JsonElement updates)
        {
            var messages = new List<VkMessageItem>();
            var messageIds = new List<long>();
            
            if (updates.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("[UpdateParseService] Получен некорректный формат updates: {Kind}", updates.ValueKind);
                return messages;
            }

            foreach (var update in updates.EnumerateArray())
            {
                var message = ParseUpdate(update);
                if (message != null)
                {
                    messages.Add(message);
                    messageIds.Add(GetMessageId(update));
                }
            }

            if (messageIds.Count > 0)
            {
                await EnrichWithAttachments(messages, messageIds);
            }

            return messages;
        }
        
        private VkMessageItem? ParseUpdate(JsonElement update)
        {
            if (update.ValueKind != JsonValueKind.Array)
                return null;
                
            var fields = update.EnumerateArray().ToArray();
            
            if (fields.Length < MIN_MESSAGE_FIELDS)
                return null;
                
            var eventType = fields[EVENT_TYPE_INDEX].GetInt32();
            if (eventType != NEW_MESSAGE_EVENT)
                return null;
            
            var flags = fields[FLAGS_INDEX].GetInt32();
            if ((flags & OUTGOING_MESSAGE_FLAG) != 0)
                return null;
                
            return new VkMessageItem
            {
                Id = fields[MESSAGE_ID_INDEX].GetInt64(),
                FromId = fields[USER_ID_INDEX].GetInt64(),
                Text = fields[TEXT_INDEX].GetString() ?? string.Empty,
                Attachments = new List<VkAttachmentItem>()
            };
        }
        
        private long GetMessageId(JsonElement update)
        {
            var fields = update.EnumerateArray().ToArray();
            return fields[MESSAGE_ID_INDEX].GetInt64();
        }
        
        private async Task EnrichWithAttachments(List<VkMessageItem> messages, List<long> messageIds)
        {
            if (messageIds.Count == 0) return;
            
            var ids = string.Join(',', messageIds);
            var url = $"https://api.vk.com/method/messages.getById?message_ids={ids}&access_token={_configuration["VK:AccessToken"]}&v=5.131";
            
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonSerializer.Deserialize<VkMessagesResponse>(response);
                
                if (apiResponse?.Response?.Items == null) return;
                    
                for (int i = 0; i < messages.Count && i < apiResponse.Response.Items.Count; i++)
                {
                    messages[i].Attachments = apiResponse.Response.Items[i].Attachments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateParseService] Ошибка получения вложений");
            }
        }
        
        private string GetLargestPhotoUrl(VkPhoto photo)
        {
            var largest = photo.Sizes
                .OrderByDescending(s => s.Width * s.Height)
                .FirstOrDefault();
                
            return largest?.Url ?? string.Empty;
        }
    }
}
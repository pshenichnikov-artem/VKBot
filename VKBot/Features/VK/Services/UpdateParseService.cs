using System.Text.Json;
using VKBot.Features.VK.Models;

namespace VKBot.Features.VK.Services
{
    public class UpdateParseService
    {
        private readonly ILogger<UpdateParseService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public UpdateParseService (ILogger<UpdateParseService> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }
        public async Task<List<VkMessage>> GetNewMessages(JsonElement updates)
        {
            List<VkMessage> vkMessages = new List<VkMessage>();
            List<long> messagesIds = new List<long> ();

            if (updates.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning($"VK updates is not an array. Actual kind: {updates.ValueKind}");
                return vkMessages;
            }

            foreach (var update in updates.EnumerateArray())
            {
                var array = update.EnumerateArray().ToList();
                var code = array[0].GetInt32();

                if (code == 4)
                {
                    if (array.Count < 7)
                    {
                        _logger.LogWarning($"VK update too short: count={array.Count}");
                        continue;
                    }
                    
                    var flags = array[2].GetInt32();
                    if ((flags & 2) != 0) // Исходящее сообщение
                    {
                        continue;
                    }
                    
                    var fromId = array[3].GetInt64();
                    var text = array[6].GetString() ?? string.Empty;

                    var message = new VkMessage
                    {
                        UserId = fromId,
                        Text = text
                    };
                    messagesIds.Add(array[4].GetInt64());
                    // Вложения (если есть)
                    if (array.Count > 7 && array[7].ValueKind == JsonValueKind.Array)
                    {
                        foreach (var attachment in array[7].EnumerateArray())
                        {
                            var type = attachment[0].GetString(); // например, "photo"
                            var payload = attachment[1]; // JsonElement с данными

                            message.Attachments.Add(new VkAttachment
                            {
                                Type = type,
                                Payload = payload
                            });
                        }
                    }
                    vkMessages.Add(message);
                }
            }

            var ids = string.Join(',', messagesIds);
            var accessToken = _configuration["VK:AccessToken"];
            var url = $"https://api.vk.com/method/messages.getById?message_ids={ids}&access_token={accessToken}&v=5.131";
            var response = await _httpClient.GetStringAsync(url);

            //TODO Достать из response нужные поля. Т.к. Update LongPoll ограничен типом обновления,
            //маской для типов, ид диалога, ид отправителя, ид сообщения, маркер обрезки текста, текст, вложения,
            //в некоторых случаях ответы и пересылка но далеко не всегда

            return vkMessages;
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Models;
using VKBot.Features.Core.Domain.Models;
using System.Text;

namespace VKBot.Features.VK.Services;

public class VkBot : IVkBot
{
    private readonly HttpClient _httpClient;
    private readonly UpdateParseService _parseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VkBot> _logger;
    private readonly string _accessToken;
    private readonly string _groupId;

    public VkBot(HttpClient httpClient, IConfiguration configuration, ILogger<VkBot> logger, UpdateParseService parseService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _parseService = parseService;
        _accessToken = _configuration["VK:AccessToken"] ?? throw new InvalidOperationException("VK AccessToken не настроен");
        _groupId = _configuration["VK:GroupId"] ?? throw new InvalidOperationException("VK GroupId не настроен");
    }

    public async Task<LongPollServer?> GetLongPollServerAsync()
    {
        var url = $"https://api.vk.com/method/messages.getLongPollServer?access_token={_accessToken}&v=5.131&group_id={_groupId}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<VkLongPollServerResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Response == null)
            {
                _logger.LogError("[VkBot] Не удалось получить LongPoll сервер");
                return null;
            }
            
            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка получения LongPoll сервера");
            return null;
        }
    }

    public async Task<List<VkMessageItem>> GetUpdatesAsync(LongPollServer server)
    {
        if (server?.Server == null || server.Key == null)
        {
            _logger.LogError("[VkBot] LongPoll сервер не инициализирован");
            return new List<VkMessageItem>();
        }

        var url = $"https://{server.Server}?act=a_check&key={server.Key}&ts={server.Ts}&wait=25";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<LongPollResponse>(response);
            
            if (result == null)
            {
                _logger.LogError("[VkBot] Пустой ответ от LongPoll");
                return new List<VkMessageItem>();
            }

            if (result.Failed > 0)
            {
                _logger.LogWarning("[VkBot] LongPoll ошибка {Failed}", result.Failed);
                return new List<VkMessageItem>();
            }

            server.Ts = result.Ts;
            var messages = await _parseService.GetNewMessages(result.Updates);
            
            if (messages.Count > 0)
            {
                _logger.LogInformation("[VkBot] Получено {Count} новых сообщений", messages.Count);
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка получения обновлений");
            return new List<VkMessageItem>();
        }
    }

    public async Task SendMessageAsync(long peerId, string message, long? replyToMessageId = null, VkKeyboard? keyboard = null, List<StateAttachment>? attachments = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("peer_id", peerId.ToString()),
            new("message", message),
            new("access_token", _accessToken),
            new("v", "5.131"),
            new("random_id", Random.Shared.Next().ToString())
        };
        
        if (replyToMessageId.HasValue)
        {
            parameters.Add(new("reply_to", replyToMessageId.Value.ToString()));
        }
        
        if (keyboard != null)
        {
            var keyboardJson = JsonSerializer.Serialize(keyboard);
            parameters.Add(new("keyboard", keyboardJson));
        }
        
        if (attachments?.Count > 0)
        {
            var attachmentStrings = attachments
                .Where(a => a.OwnerId.HasValue && a.MediaId.HasValue)
                .Select(a => $"{a.Type}{a.OwnerId}_{a.MediaId}")
                .ToList();
            
            if (attachmentStrings.Count > 0)
            {
                parameters.Add(new("attachment", string.Join(",", attachmentStrings)));
            }
        }

        

        try
        {
            var content = new FormUrlEncodedContent(parameters);

            var response = await _httpClient.PostAsync("https://api.vk.com/method/messages.send", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("[VkBot] Ответ VK API: {Response}", responseText);
            
            if (responseText.Contains("\"error\""))
            {
                _logger.LogError("[VkBot] Ошибка отправки сообщения получателю {PeerId}. Ответ: {Response}", peerId, responseText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка отправки сообщения получателю {PeerId}", peerId);
        }
    }
}
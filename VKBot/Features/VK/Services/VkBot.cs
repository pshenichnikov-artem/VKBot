using System.Text.Json;
using Microsoft.Extensions.Logging;
using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Models;

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
            var result = JsonSerializer.Deserialize<VkApiResponse<LongPollServer>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

    public async Task<List<VkMessage>> GetUpdatesAsync(LongPollServer server)
    {
        if (server?.Server == null || server.Key == null)
        {
            _logger.LogError("[VkBot] LongPoll сервер не инициализирован");
            return new List<VkMessage>();
        }

        var url = $"https://{server.Server}?act=a_check&key={server.Key}&ts={server.Ts}&wait=25";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<LongPollResponse>(response);
            
            if (result == null)
            {
                _logger.LogError("[VkBot] Пустой ответ от LongPoll");
                return new List<VkMessage>();
            }

            if (result.Failed > 0)
            {
                _logger.LogWarning("[VkBot] LongPoll ошибка {Failed}", result.Failed);
                return new List<VkMessage>();
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
            return new List<VkMessage>();
        }
    }

    public async Task SendMessageAsync(long userId, string message)
    {
        var url = $"https://api.vk.com/method/messages.send?user_id={userId}&message={Uri.EscapeDataString(message)}&access_token={_accessToken}&v=5.131&random_id={Random.Shared.Next()}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            
            if (response.Contains("\"error\""))
            {
                _logger.LogError("[VkBot] Ошибка отправки сообщения пользователю {UserId}. Ответ: {Response}", userId, response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка отправки сообщения пользователю {UserId}", userId);
        }
    }
}
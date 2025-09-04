using System.Text.Json;
using Microsoft.Extensions.Logging;
using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Models;

namespace VKBot.Features.VK.Services;

/// <summary>
/// Класс для проверки новых сообщений в вк боте и отправки сообщений
/// </summary>
public class VkBot : IVkBot
{
    private readonly HttpClient _httpClient;
    private readonly UpdateParseService _parseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VkBot> _logger;

    public VkBot(HttpClient httpClient, IConfiguration configuration, ILogger<VkBot> logger, UpdateParseService parseService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _parseService = parseService;
    }

    public async Task<LongPollServer?> GetLongPollServerAsync()
    {
        var accessToken = _configuration["VK:AccessToken"];
        var groupId = _configuration["VK:GroupId"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogError("VK AccessToken is missing.");
            return null;
        }

        var url = $"https://api.vk.com/method/messages.getLongPollServer?access_token={accessToken}&v=5.131&group_id={groupId}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            _logger.LogError("VK raw response: " + response);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            var result = JsonSerializer.Deserialize<VkApiResponse<LongPollServer>>(response, options);

            if (result?.Response == null)
            {
                _logger.LogError("VK API returned null LongPollServer.");
                return null;
            }
            _logger.LogError($"VK Long Poll: server={result.Response.Server}, key={result.Response.Key}, ts={result.Response.Ts}");
            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get LongPollServer.");
            return null;
        }
    }

    /// <summary>
    /// Получить новые события
    /// </summary>
    public async Task<List<VkMessage>> GetUpdatesAsync(LongPollServer server)
    {
        var messages = new List<VkMessage>();
        if (server == null || string.IsNullOrEmpty(server.Server) || string.IsNullOrEmpty(server.Key))
        {
            _logger.LogError("LongPollServer is not properly initialized.");
            return messages;
        }

        var url = $"https://{server.Server}?act=a_check&key={server.Key}&ts={server.Ts}&wait=25";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            _logger.LogError("VK Long Poll raw response: " + response);
            if(response == null)
            {
                _logger.LogError("Long Poll Response Json its null");
                return messages;
            }

            _logger.LogError("1");
            var result = JsonSerializer.Deserialize<LongPollResponse>(response);
            _logger.LogWarning($"VK LongPoll failed={result.Failed}");
            if (result.Failed > 0)
            {
                _logger.LogWarning($"VK LongPoll failed={result.Failed}. Reinitializing...");
                server = await GetLongPollServerAsync();
                return new List<VkMessage>();
            }

            if (result == null)
            {
                _logger.LogError("Long Poll returned null");
            }

            server.Ts = result.Ts;
            _logger.LogError("2 " + server.Ts);
            _logger.LogError("Я тут");

            messages = await _parseService.GetNewMessages(result.Updates);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while polling VK updates.");
            return messages;
        }
    }

    /// <summary>
    /// Отправить сообщение пользователю
    /// </summary>
    public async Task SendMessageAsync(long userId, string message)
    {
        var accessToken = _configuration["VK:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogError("VK AccessToken is missing.");
            return;
        }

        var url = $"https://api.vk.com/method/messages.send?user_id={userId}&message={Uri.EscapeDataString(message)}&access_token={accessToken}&v=5.131&random_id={Random.Shared.Next()}";

        try
        {
            await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send message to user {userId}.");
        }
    }
}
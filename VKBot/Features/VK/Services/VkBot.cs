using System.Text.Json;
using VKBot.Features.VK.Interfaces;
using VKBot.Features.VK.Models;

namespace VKBot.Features.VK.Services;

/// <summary>
/// Класс для проверки новых сообщений в вк боте и отправки сообщений
/// </summary>
public class VkBot : IVkBot
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public VkBot(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<LongPollServer> GetLongPollServerAsync()
    {
        var accessToken = _configuration["VK:AccessToken"];
        var url = $"https://api.vk.com/method/messages.getLongPollServer?access_token={accessToken}&v=5.131&group_id=1";
        var response = await _httpClient.GetStringAsync(url);
        var result = JsonSerializer.Deserialize<VkApiResponse<LongPollServer>>(response);
        return result.Response;
    }

   /// <summary>
   /// Получить новые события
   /// </summary>
   /// <param name="server"></param>
   /// <returns></returns>
    public async Task<VkMessage[]> GetUpdatesAsync(LongPollServer server)
    {
        var url = $"{server.Server}?act=a_check&key={server.Key}&ts={server.Ts}&wait=25";
        var response = await _httpClient.GetStringAsync(url);
        var result = JsonSerializer.Deserialize<LongPollResponse>(response);
        
        server.Ts = result.Ts;
        
        return result.Updates?
            .Where(u => u.Type == "message_new")
            .Select(u => new VkMessage 
            { 
                UserId = u.Object.Message.FromId, 
                Text = u.Object.Message.Text 
            })
            .ToArray() ?? Array.Empty<VkMessage>();
    }

    /// <summary>
    /// Отправить сообщение пользователю
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task SendMessageAsync(long userId, string message)
    {
        var accessToken = _configuration["VK:AccessToken"];
        var url = $"https://api.vk.com/method/messages.send?user_id={userId}&message={Uri.EscapeDataString(message)}&access_token={accessToken}&v=5.131&random_id={Random.Shared.Next()}";
        await _httpClient.GetStringAsync(url);
    }
}


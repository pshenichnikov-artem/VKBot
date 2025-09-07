using System.Text.Json;
using Microsoft.Extensions.Logging;
using VKBot.Features.Core.Domain.Models;
using System.Text;
using Microsoft.Extensions.Configuration;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Application.Interfaces;

namespace VKBot.Features.VK.Application.Services;

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

    public async Task<long?> SendMessageAsync(long peerId, string message, long? replyToMessageId = null, VkKeyboard? keyboard = null, List<StateAttachment>? attachments = null)
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
            var attachmentStrings = new List<string>();
            
            foreach (var attachment in attachments)
            {
                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    // Отправляем файл отдельно
                    await SendDocumentAsync(peerId, attachment.FilePath, message);
                    return null; // Возвращаем null, так как сообщение уже отправлено
                }
                else if (attachment.OwnerId.HasValue && attachment.MediaId.HasValue)
                {
                    attachmentStrings.Add($"{attachment.Type}{attachment.OwnerId}_{attachment.MediaId}");
                }
            }
            
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

            try
            {
                var successResponse = JsonSerializer.Deserialize<VkSendMessageResponse>(responseText);
                return successResponse?.MessageId;
            }
            catch
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<VkErrorResponse>(responseText);
                    _logger.LogError("[VkBot] Ошибка VK API: {ErrorCode} - {ErrorMessage}",
                        errorResponse?.Error.ErrorCode, errorResponse?.Error.ErrorMessage);
                }
                catch
                {
                    _logger.LogError("[VkBot] Неизвестный формат ответа: {Response}", responseText);
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка отправки сообщения получателю {PeerId}", peerId);
            return null;
        }
    }

    public async Task<long?> ForwardMessageAsync(long peerId, long messageId, string? additionalMessage = null, VkKeyboard? keyboard = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("peer_id", peerId.ToString()),
            new("forward_messages", messageId.ToString()),
            new("access_token", _accessToken),
            new("v", "5.131"),
            new("random_id", Random.Shared.Next().ToString())
        };
        
        if (!string.IsNullOrEmpty(additionalMessage))
        {
            parameters.Add(new("message", additionalMessage));
        }
        
        if (keyboard != null)
        {
            var keyboardJson = JsonSerializer.Serialize(keyboard);
            parameters.Add(new("keyboard", keyboardJson));
        }

        try
        {
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("https://api.vk.com/method/messages.send", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            var successResponse = JsonSerializer.Deserialize<VkSendMessageResponse>(responseText);
            return successResponse?.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка пересылки сообщения {MessageId} получателю {PeerId}", messageId, peerId);
            return null;
        }
    }

    public async Task<long?> SendDocumentAsync(long peerId, string filePath, string message)
    {
        try
        {
            // 1. Получаем URL для загрузки
            var uploadUrlResponse = await _httpClient.GetStringAsync(
                $"https://api.vk.com/method/docs.getMessagesUploadServer?access_token={_accessToken}&v=5.131&peer_id={peerId}");
            
            var uploadUrlData = JsonSerializer.Deserialize<JsonElement>(uploadUrlResponse);
            var uploadUrl = uploadUrlData.GetProperty("response").GetProperty("upload_url").GetString();

            // 2. Загружаем файл
            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var uploadResponse = await _httpClient.PostAsync(uploadUrl, form);
            var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
            var uploadData = JsonSerializer.Deserialize<JsonElement>(uploadResult);
            
            var file = uploadData.GetProperty("file").GetString();

            // 3. Сохраняем документ
            var saveResponse = await _httpClient.GetStringAsync(
                $"https://api.vk.com/method/docs.save?access_token={_accessToken}&v=5.131&file={file}&title={Path.GetFileNameWithoutExtension(filePath)}");
            
            var saveData = JsonSerializer.Deserialize<JsonElement>(saveResponse);
            var responseProperty = saveData.GetProperty("response");
            
            JsonElement doc;
            if (responseProperty.TryGetProperty("doc", out var docProperty))
            {
                doc = docProperty;
            }
            else
            {
                // Иногда VK возвращает массив
                doc = responseProperty[0];
            }
            
            var ownerId = doc.GetProperty("owner_id").GetInt64();
            var docId = doc.GetProperty("id").GetInt64();

            // 4. Отправляем сообщение с документом
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("peer_id", peerId.ToString()),
                new("message", message),
                new("attachment", $"doc{ownerId}_{docId}"),
                new("access_token", _accessToken),
                new("v", "5.131"),
                new("random_id", Random.Shared.Next().ToString())
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("https://api.vk.com/method/messages.send", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            var successResponse = JsonSerializer.Deserialize<VkSendMessageResponse>(responseText);
            return successResponse?.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VkBot] Ошибка отправки документа {FilePath} получателю {PeerId}", filePath, peerId);
            return null;
        }
    }
}
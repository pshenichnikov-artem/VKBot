using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Enums;

namespace VKBot.Features.Host.BackgroundServices;

public class AdminSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AdminSyncService(IServiceProvider serviceProvider, ILogger<AdminSyncService> logger, 
        IConfiguration configuration, HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                await SyncAdmins();
                await Task.Delay(TimeSpan.FromHours(24 - DateTime.UtcNow.AddHours(3).Hour), stoppingToken); // Раз в сутки 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка AdminSyncService");
            }
        } while (true);
    }

    private async Task SyncAdmins()
    {
        try
        {
            var accessToken = _configuration["VK:AccessToken"];
            var groupId = _configuration["VK:GroupId"];
            
            var url = $"https://api.vk.com/method/groups.getMembers?group_id={groupId}&filter=managers&access_token={accessToken}&v=5.131";
            var response = await _httpClient.GetStringAsync(url);
            _logger.LogInformation(response);
            var apiResponse = JsonSerializer.Deserialize<VkAdminsResponse>(response);
            
            if (apiResponse?.Response?.Items == null)
            {
                _logger.LogError("Не удалось получить список админов из VK");
                return;
            }

            var vkAdminIds = apiResponse.Response.Items.Select(a => a.Id).ToHashSet();
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Получаем текущих админов из БД
            var dbAdmins = await context.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.Admin.ToString())
                .ToListAsync();
            
            // Удаляем админов, которых нет в VK
            var adminsToRemove = dbAdmins.Where(a => !vkAdminIds.Contains(a.VkUserId)).ToList();
            foreach (var admin in adminsToRemove)
            {
                admin.Role = UserRole.Student.ToString();
                _logger.LogInformation("Удален админ: {UserId}", admin.VkUserId);
            }
            
            // Добавляем новых админов
            var existingAdminIds = dbAdmins.Select(a => a.VkUserId).ToHashSet();
            var newAdminIds = vkAdminIds.Except(existingAdminIds).ToList();
            
            if (newAdminIds.Any())
            {
                // Получаем имена админов из VK
                var userIds = string.Join(",", newAdminIds);
                var usersUrl = $"https://api.vk.com/method/users.get?user_ids={userIds}&access_token={accessToken}&v=5.131";
                var usersResponse = await _httpClient.GetStringAsync(usersUrl);
                var usersData = JsonSerializer.Deserialize<VkUsersResponse>(usersResponse);
                
                foreach (var adminId in newAdminIds)
                {
                    var vkUser = usersData?.Response?.FirstOrDefault(u => u.Id == adminId);
                    var fullName = vkUser != null ? $"{vkUser.FirstName} {vkUser.LastName}" : "Администратор";
                    
                    var existingUser = await context.Users
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.VkUserId == adminId);
                    
                    if (existingUser != null)
                    {
                        existingUser.FullName = fullName;
                        existingUser.Role = UserRole.Admin.ToString();
                        existingUser.IsConfirmed = true;
                        existingUser.IsBlocked = false;
                        existingUser.IsDeleted = false;
                    }
                    else
                    {
                        var newAdmin = new User
                        {
                            VkUserId = adminId,
                            FullName = fullName,
                            Role = UserRole.Admin.ToString(),
                            IsConfirmed = true,
                            IsBlocked = false,
                            IsDeleted = false
                        };
                        context.Users.Add(newAdmin);
                    }
                    
                    _logger.LogInformation("Добавлен админ: {UserId} - {FullName}", adminId, fullName);
                }
            }
            
            await context.SaveChangesAsync();
            _logger.LogInformation("Синхронизация админов завершена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка синхронизации админов");
        }
    }
}

public class VkAdminsResponse
{
    [JsonPropertyName("response")]
    public VkAdminsData Response { get; set; } = null!;
}

public class VkAdminsData
{
    [JsonPropertyName("items")]
    public List<VkAdmin> Items { get; set; } = new();
}

public class VkAdmin
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

public class VkUsersResponse
{
    [JsonPropertyName("response")]
    public List<VkUser> Response { get; set; } = new();
}

public class VkUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;
}
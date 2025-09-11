using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using VKBot.Features.Core.Application.Interfaces;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;

namespace VKBot.Features.Core.Application.Services.MessageContentProviders;

public class QuestionContentProvider : IMessageContentProvider
{
    private readonly IServiceProvider _serviceProvider;

    public QuestionContentProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string GetMessageType() => PayloadType.Question.ToString();

    public async Task<StateResult> GenerateMessageContent(Message message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sender = await context.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.VkUserId == message.SenderId);

        var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload!);
        var questionText = payload.GetProperty("text").GetString();

        var text = $"❓ У вас новые вопросы от студентов\n\n" +
                   $"Используйте /questions для просмотра";

        var keyboard = VkKeyboard.Create(inline: true);
        keyboard.AddRow();
        keyboard.AddButton("/questions", VkButtonColor.Primary);

        return StateResult.Success(text, StateAction.End, keyboard: keyboard);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKBot.Features.Core.Data;
using VKBot.Features.Core.Domain.Entities;
using VKBot.Features.Core.Domain.Enums;
using VKBot.Features.Core.Domain.Models;
using VKBot.Features.Core.Enums;
using VKBot.Features.VK.Domain.Models;
using VKBot.Features.VK.Enums;
using System.Text.Json;

namespace VKBot.Features.Core.Application.States;

public class AnswerQuestionState : BaseState
{
    private List<Message> _questions = new();
    private int _currentQuestionIndex = 0;

    public AnswerQuestionState(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override string Description => _step switch
    {
        0 => "❓ Ответы на вопросы\nПросмотр вопросов от студентов",
        1 => "🎯 Начало обработки\nПереход к пошаговому рассмотрению",
        2 => "👀 Рассмотрение вопроса\nВыбор действия с вопросом",
        3 => "✍️ Написание ответа\nСоставление ответа студенту",
        _ => "❓ Неизвестный шаг"
    };

    public override bool IsEntryPoint => true;
    public override string? Command => "/questions";
    public override UserRole[] AllowedRoles => new[] { UserRole.Admin };

    protected override Dictionary<int, Type[]> AvailableStates => new();

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return _step switch
        {
            0 => await ShowQuestions(),
            1 => ProcessMainAction(message),
            2 => await ProcessQuestionAction(message),
            3 => await ProcessAnswer(message),
            _ => StateResult.Success("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowQuestions()
    {
        _step = 1;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _questions = await context.Messages
            .Include(m => m.Sender)
            .Where(m => m.Payload != null && m.Payload.Contains("\"type\":\"question\"") && 
                   !m.Payload.Contains("\"answered\":true"))
            .OrderBy(m => m.Id)
            .ToListAsync();

        if (!_questions.Any())
        {
            return StateResult.Success("😌 На данный момент нет новых вопросов", StateAction.End);
        }

        var questionsList = $"❓ Новые вопросы ({_questions.Count}):\n\n";
        foreach (var question in _questions.Take(5))
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(question.Payload!);
            var text = payload.GetProperty("text").GetString();
            questionsList += $"👤 {question.Sender?.FullName}: {text?.Substring(0, Math.Min(50, text.Length))}...\n";
        }

        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("🚀 Начать отвечать", VkButtonColor.Primary);

        return StateResult.Success(questionsList, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessMainAction(UserMessage message)
    {
        if (message.Text?.ToLower().Contains("отвечать") == true)
        {
            _step = 2;
            _currentQuestionIndex = 0;
            return ShowCurrentQuestion();
        }

        return StateResult.Success("⚠️ Пожалуйста, используйте кнопки для выбора", StateAction.Stay);
    }

    private async Task<StateResult> ProcessQuestionAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();

        switch (action)
        {
            case "ответить":
                _step = 3;
                return StateResult.Success("✍️ Напишите ответ на вопрос:", StateAction.Stay);
            case "удалить":
                await DeleteQuestion();
                break;
            case var s when s.Contains("завершить"):
                return StateResult.Success("✅ Ответы на вопросы завершены", StateAction.End);
            case "пропустить":
                break;
        }

        _currentQuestionIndex++;

        if (_currentQuestionIndex >= _questions.Count)
        {
            return StateResult.Success("✅ Все вопросы обработаны", StateAction.End);
        }

        return ShowCurrentQuestion();
    }

    private async Task<StateResult> ProcessAnswer(UserMessage message)
    {
        var answerText = message.Text;
        if (string.IsNullOrEmpty(answerText))
        {
            return StateResult.Success("⚠️ Ответ не может быть пустым. Пожалуйста, напишите ответ:", StateAction.Stay);
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentQuestion = _questions[_currentQuestionIndex];
        var msg = new Message
        {
            SenderId = message.UserId,
            ReplyToMessageId = currentQuestion.Id,
            Payload = $"{{\"type\":\"answer\",\"text\":\"{answerText.Replace("\"", "\\\"")}\"}}"
        };
        context.Messages.Add(msg);
        await context.SaveChangesAsync();

        context.MessageDeliveries.Add(new MessageDelivery
        {
            MessageId = msg.Id,
            RecipientId = currentQuestion.SenderId!.Value,
            DeliveryStatus = MessageStatus.Pending.ToString().ToLower(),
            DispatchTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var questionToUpdate = await context.Messages.FirstAsync(m => m.Id == currentQuestion.Id);
        var payload = JsonSerializer.Deserialize<JsonElement>(questionToUpdate.Payload!);
        var text = payload.GetProperty("text").GetString();
        questionToUpdate.Payload = $"{{\"type\":\"question\",\"text\":\"{text}\",\"answered\":true}}";
        await context.SaveChangesAsync();

        _currentQuestionIndex++;
        _step = 2;

        if (_currentQuestionIndex >= _questions.Count)
        {
            return StateResult.Success("✅ Все вопросы обработаны", StateAction.End);
        }

        return ShowCurrentQuestion();
    }

    private StateResult ShowCurrentQuestion()
    {
        var question = _questions[_currentQuestionIndex];
        var payload = JsonSerializer.Deserialize<JsonElement>(question.Payload!);
        var text = payload.GetProperty("text").GetString();

        var questionInfo = $"❓ Вопрос {_currentQuestionIndex + 1} из {_questions.Count}\n\n👤 От: {question.Sender?.FullName}\n💬 Текст: {text}";

        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("✍️ Ответить", VkButtonColor.Positive);
        keyboard.AddButton("❌ Удалить", VkButtonColor.Negative);
        keyboard.AddRow();
        keyboard.AddButton("⏭️ Пропустить", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("✅ Завершить", VkButtonColor.Secondary);

        return StateResult.Success(questionInfo, StateAction.Stay, keyboard: keyboard);
    }

    private async Task DeleteQuestion()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var currentQuestion = _questions[_currentQuestionIndex];
        var dbQuestion = await context.Messages.FirstAsync(m => m.Id == currentQuestion.Id);
        context.Messages.Remove(dbQuestion);
        await context.SaveChangesAsync();
    }
}
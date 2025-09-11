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
using VKBot.Features.VK.Application.Middleware.Attributes;
using VKBot.Features.VK.Application.Interfaces;

namespace VKBot.Features.Core.Application.States;

[State("вопросы", UserRole.Admin)]
[Description(0, "❓ Обработка вопросов студентов\nКоманда для просмотра и ответов на вопросы от студентов. Показывает список новых вопросов с возможностью ответить, удалить или пропустить.")]
[Description(1, "📋 Начало обработки\nНажмите 'Перейти к ответам' для пошагового рассмотрения вопросов")]
[Description(2, "📄 Рассмотрение вопроса\nВыберите действие: ответить, удалить или пропустить вопрос")]
[Description(3, "📝 Написание ответа\nНапишите подробный ответ на вопрос студента")]
public class AnswerQuestionState : BaseState
{
    [NonSerialized]
    private readonly AppDbContext _context;
    [NonSerialized]
    private readonly IVkBot _vkBot;
    private List<Message> _questions = new();
    private int _currentQuestionIndex = 0;

    public AnswerQuestionState(AppDbContext context, IVkBot vkBot) 
    { 
        _context = context;
        _vkBot = vkBot;
    }

    public override async Task<StateResult> ExecuteAsync(UserMessage message)
    {
        return Step switch
        {
            0 => await ShowQuestions(),
            1 => ProcessMainAction(message),
            2 => await ProcessQuestionAction(message),
            3 => await ProcessAnswer(message),
            _ => new StateResult("Ошибка", StateAction.End)
        };
    }

    private async Task<StateResult> ShowQuestions()
    {
        Step = 1;

        _questions = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.Payload != null && m.Payload.Contains($"\"type\":\"{PayloadType.Question}\"") && 
                   !m.Payload.Contains("\"answered\":true"))
            .OrderByDescending(m => m.Id)
            .ToListAsync();

        if (!_questions.Any())
        {
            return new StateResult("❓ Нет новых вопросов", StateAction.End);
        }

        var questionsList = $"Вопросы ({_questions.Count}):\n";
        foreach (var question in _questions.Take(5))
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(question.Payload!);
            var text = payload.GetProperty("text").GetString();
            var truncatedText = text?.Length > 30 ? text.Substring(0, 30) + "..." : text;
            var time = question.CreatedAt.AddHours(3).ToString("dd.MM HH:mm") ?? "";
            questionsList += $"От {question.Sender?.FullName} ({time}): {truncatedText}\n";
        }

        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Перейти к ответам", VkButtonColor.Primary);

        return new StateResult(questionsList, StateAction.Stay, keyboard: keyboard);
    }

    private StateResult ProcessMainAction(UserMessage message)
    {
        if (message.Text?.ToLower().Trim() == "перейти к ответам")
        {
            Step = 2;
            _currentQuestionIndex = 0;
            return ShowCurrentQuestion();
        }

        return new StateResult("❌ Используйте кнопки для выбора", StateAction.Stay);
    }

    private async Task<StateResult> ProcessQuestionAction(UserMessage message)
    {
        var action = message.Text?.ToLower().Trim();

        switch (action)
        {
            case "ответить":
                Step = 3;
                return new StateResult("📝 Введите ответ на вопрос:", StateAction.Stay);
            case "удалить":
                await DeleteQuestion();
                break;
            case "закончить ответы на вопросы":
                return new StateResult("✅ Ответы на вопросы завершены", StateAction.End);
            case "пропустить":
                break;
        }

        _currentQuestionIndex++;

        if (_currentQuestionIndex >= _questions.Count)
        {
            return new StateResult("Все вопросы обработаны", StateAction.End);
        }

        return ShowCurrentQuestion();
    }

    private async Task<StateResult> ProcessAnswer(UserMessage message)
    {
        var answerText = message.Text;
        if (string.IsNullOrEmpty(answerText))
        {
            return new StateResult("❌ Ответ обязателен\n📝 Напишите ответ:", StateAction.Stay);
        }

        var currentQuestion = _questions[_currentQuestionIndex];
        
        // Отправляем ответ напрямую пользователю
        await _vkBot.SendMessageAsync(currentQuestion.SenderId!.Value, $"❓ Ответ на ваш вопрос:\n\n{answerText}");//TODO ответ на сообщение пользователя

        // Отмечаем вопрос как отвеченный
        var questionToUpdate = await _context.Messages.FirstAsync(m => m.Id == currentQuestion.Id);
        var payload = JsonSerializer.Deserialize<JsonElement>(questionToUpdate.Payload!);
        var text = payload.GetProperty("text").GetString();
        questionToUpdate.Payload = $"{{\"type\":\"{PayloadType.Question}\",\"text\":\"{text}\",\"answered\":true}}";
        await _context.SaveChangesAsync();

        _currentQuestionIndex++;
        Step = 2;

        if (_currentQuestionIndex >= _questions.Count)
        {
            return new StateResult("Все вопросы обработаны", StateAction.End);
        }

        return ShowCurrentQuestion();
    }

    private StateResult ShowCurrentQuestion()
    {
        var question = _questions[_currentQuestionIndex];
        var payload = JsonSerializer.Deserialize<JsonElement>(question.Payload!);
        var text = payload.GetProperty("text").GetString();

        var questionInfo = $"Вопрос {_currentQuestionIndex + 1} из {_questions.Count}:\n";
        questionInfo += $"От: {question.Sender?.FullName}\n";
        questionInfo += $"Текст: {text}";

        var keyboard = VkKeyboard.Create(false, true);
        keyboard.AddRow();
        keyboard.AddButton("Ответить", VkButtonColor.Positive);
        keyboard.AddButton("Удалить", VkButtonColor.Negative);
        keyboard.AddRow();
        keyboard.AddButton("Пропустить", VkButtonColor.Primary);
        keyboard.AddRow();
        keyboard.AddButton("Закончить ответы на вопросы", VkButtonColor.Secondary);

        return new StateResult(questionInfo, StateAction.Stay, keyboard: keyboard);
    }

    private async Task DeleteQuestion()
    {
        var currentQuestion = _questions[_currentQuestionIndex];
        var dbQuestion = await _context.Messages.FirstAsync(m => m.Id == currentQuestion.Id);
        _context.Messages.Remove(dbQuestion);
        await _context.SaveChangesAsync();
    }
}

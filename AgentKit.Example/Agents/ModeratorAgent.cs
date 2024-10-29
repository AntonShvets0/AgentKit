using System.Reflection;
using AgentKit.Abstractions;
using AgentKit.Example.Models;
using AgentKit.Example.Models.Moderator;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Services;
using AgentKit.Services.Context;

namespace AgentKit.Example.Agents;

/// <summary>
/// Простейший агент, который модерирует сообщение пользователя и возвращает, содержит ли сообщение вредоносный контент
/// </summary>
public class ModeratorAgent(
    
    IInferenceFactory inferenceFactory,
    int severity = 75
    
    )
    : AgentBase<ModeratorRequest, ModeratorVerdict>(
        inferenceFactory, 
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
{
    private readonly int _severity = severity;
    
    public override async Task<ModeratorVerdict> SendRequestAsync(ModeratorRequest request, CancellationToken? cancellationToken = null)
    {
        var inference = InferenceFactory.CreateClient(
            InferenceType.Middle,
            new DisabledConversationContext()
        ); // Создаем inference среднего размера без контекста

        var response = await inference.CompleteChatAsync<ModeratorResponse>(
            await GetPromptAsync("moderate", new
            {
                text = request.Text
            }),
            new InferenceChatCompletionOptions(),
            [],
            this
        );
        if (response == null) 
            return new ModeratorVerdict(true); // Возможно, LLM вернул нам пустой ответ из-за цензуры провайдера, касаемого вредоносного контента

        return new ModeratorVerdict(

            response.Ad >= _severity ||
            response.Nsfw >= _severity ||
            response.HateSpeech >= _severity

        );
    }
}
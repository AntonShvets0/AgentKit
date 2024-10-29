using System.Reflection;
using AgentKit.Abstractions;
using AgentKit.Example.Models.AutoTranslator;
using AgentKit.Example.Models.LogAnalyser;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Services;
using AgentKit.Services.Context;

namespace AgentKit.Example.Agents;

/// <summary>
/// Автономный агент, который на фоне проверяет наличие новых постов в официальном канале - и сразу переводит их на другие языки
/// </summary>
public class AutoTranslatorAgent : ProactiveAgent<AutoTranslatorResponse>
{
    private string[] _languages;
    protected override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    public AutoTranslatorAgent(IInferenceFactory inferenceFactory, Func<AutoTranslatorResponse, Task> onNewPost,
        string[] translatorLanguages) : base(
        inferenceFactory,
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
    {
        _languages = translatorLanguages;
        SubscribeEvent("onNewPost", onNewPost);
    }
    
    public override ValueTask<bool> CanExecuteAsync(int iteration)
    {
        // В реальной программе, мы бы проверяли наличие постов 
        // Если новых постов нет - агент дальше ждет.
        return new ValueTask<bool>(true);
    }

    public override async Task<ProactiveResponse<AutoTranslatorResponse>> SendRequestAsync(HostedRequest request, CancellationToken? cancellationToken = null)
    {
        var post = ""; // В реальной бы программе мы бы получали последний пост
        
        var inference = InferenceFactory.CreateClient(
            InferenceType.Big,
            new DisabledConversationContext()
        ); // Создаем inference среднего размера без контекста

        var response = await inference.CompleteChatAsync<TranslatorResponse>(
            await GetPromptAsync("translate", new
            {
                languages = string.Join(", ", _languages),
                text = post
            }),
            new InferenceChatCompletionOptions(Temperature: 0.5f),
            [],
            this
        );
        if (response == null)
            throw new HostedException();

        return new ProactiveResponse<AutoTranslatorResponse>
        {
            Event = "onNewPost",
            Response = new AutoTranslatorResponse
            {
                TranslatedText = response.TranslatedText,
                SourceText = post
            }
        };
    }
}
using System.Reflection;
using AgentKit.Abstractions;
using AgentKit.Example.Models.AutoTranslator;
using AgentKit.Example.Models.LogAnalyser;
using AgentKit.Example.Models.Moderator;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Services;
using AgentKit.Services.Context;

namespace AgentKit.Example.Agents;

/// <summary>
/// Автономный агент, который на фоне проверяет логи и регистрирует аномалии
/// </summary>
public class LogAnalyserAgent(IInferenceFactory inferenceFactory)
    : HostedAgent<LogAnalyserWarning>(inferenceFactory, 
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
{
    protected override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    public override ValueTask<bool> CanExecuteAsync(int iteration)
    {
        // В реальной программе, мы бы проверяли наличие логов 
        // Если новых логов нет - агент дальше ждет.
        return new ValueTask<bool>(true);
    }

    public override async Task<LogAnalyserWarning?> SendRequestAsync(HostedRequest request, CancellationToken? cancellationToken = null)
    {
        var log = ""; // в реальной программе мы бы получали лог из каких-то источников
        
        var inference = InferenceFactory.CreateClient(
            InferenceType.Big,
            new DisabledConversationContext()
        ); // Создаем inference среднего размера без контекста

        var response = await inference.CompleteChatAsync<LogAnalyser>(
            await GetPromptAsync("logAnalyse", new
            {
                log = log
            }),
            new InferenceChatCompletionOptions(Temperature: 0.1f),
            [],
            this
        );
        if (response == null)
            throw new HostedException(); // HostedException бросается, если в этот раз надо проигнорировать запись в Event log 

        if (response.CriticalLevel < 10)
            throw new HostedException();

        return new LogAnalyserWarning
        {
            CriticalLevel = response.CriticalLevel,
            WarningReason = response.WarningReason,
            Log = log
        };
    }
}
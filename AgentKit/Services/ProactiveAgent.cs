using AgentKit.Abstractions;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services;

/// <summary>
/// Агент, который работает на фоне и самостоятельно вызывает нужные event, которые были предварительно зарегистрированы
/// </summary>
public abstract class ProactiveAgent<TResponse> : HostedAgent<ProactiveResponse<TResponse>>
    where TResponse : class
{
    private Dictionary<string, Func<TResponse, Task>> _events = new();

    protected ProactiveAgent(
        IInferenceFactory inferenceFactory,
        IPromptProvider provider
        ) : base(inferenceFactory, provider)
    {
    }

    public void SubscribeEvent(string id, Func<TResponse, Task> call)
    {
        _events[id] = call;
    }

    protected override async Task PublishEventAsync(ProactiveResponse<TResponse> response)
    {
        await base.PublishEventAsync(response);

        if (!_events.TryGetValue(response.Event, out var ev))
            throw new KeyNotFoundException($"{GetType().Name} agent is returned unknown event");
        
        await ev.Invoke(response.Response);
    }
}
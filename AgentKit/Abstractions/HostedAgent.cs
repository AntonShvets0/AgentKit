using System.Collections.Concurrent;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Abstractions;

/// <summary>
/// This type of agent starts once and works by itself - in the background until it receives a command to stop.
/// HostedAgent collects all received information in Queue
/// </summary>
public abstract class HostedAgent<TResponse> : AgentBase<HostedRequest, TResponse>
    where TResponse : class
{
    private bool _isRunnig;
    
    public ConcurrentQueue<TResponse> Events { get; set; }
    
    protected abstract TimeSpan Timeout { get; set; }
    protected HostedAgent(
        IInferenceFactory inferenceFactory,
        IPromptProvider prompt) : base(inferenceFactory, prompt) {}

    public virtual ValueTask<bool> CanExecuteAsync(int iteration)
        => ValueTask.FromResult<bool>(true);
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_isRunnig)
            throw new HostAlreadyStartedException();
        
        _ = Task.Run(async () =>
        {
            _isRunnig = true;
            var iteration = -1;
            while (!cancellationToken.IsCancellationRequested && !IsDisposed)
            {
                if (!await CanExecuteAsync(iteration))
                {
                    await Task.Delay(Timeout, cancellationToken: cancellationToken);
                    continue;
                }

                TResponse response;
                try
                {
                    response = await SendRequestAsync(new HostedRequest(++iteration), cancellationToken);
                }
                catch (HostedException)
                {
                    continue;
                }
                
                await PublishEventAsync(response);
                
                await Task.Delay(Timeout, cancellationToken: cancellationToken);
            }
        }, cancellationToken);
    }

    protected virtual Task PublishEventAsync(TResponse response)
    {
        Events.Enqueue(response);
        return Task.CompletedTask;
    }
}
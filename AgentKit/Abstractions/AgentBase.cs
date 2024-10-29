using System.Reflection;
using AgentKit.Exceptions;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Abstractions;

public abstract class AgentBase<TRequest, TResponse> : IAgent, IDisposable
    where TResponse : class
{
    protected IInferenceFactory InferenceFactory { get; }
    
    protected IPromptProvider Prompt { get; }
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    protected bool IsDisposed { get; private set; }

    protected AgentBase(
        IInferenceFactory inferenceFactory,
        IPromptProvider prompt
        )
    {
        InferenceFactory = inferenceFactory;
        Prompt = prompt;
    }
    
    public AgentState State { get; protected set; }

    public async Task<object> SendObjectRequest(object request, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;
        
        ThrowIfDisposed();
            
        if (request is not TRequest castedRequest) 
            throw new ArgumentException(
                $"Expected request of type {typeof(TRequest).Name}, but got {request?.GetType().Name ?? "null"}");
        
        return await SendRequestAsync(castedRequest, (CancellationToken)cancellationToken);
    }
    
    protected async Task<string> GetPromptAsync(string id, object parameters)
    {
        ThrowIfDisposed();
        
        if (State != AgentState.Running)
            throw new AgentIsNotStartedYetException("Agent must be in Running state to get prompts");

        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Prompt id cannot be null or empty", nameof(id));

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var template = await Prompt.GetPromptAsync(id);
        var variables = new Dictionary<string, string>();
            
        foreach (var property in GetObjectProperties(parameters))
        {
            variables[property.Key] = property.Value;
        }

        var prompt = template.Content;
        foreach (var (key, value) in variables)
        {
            prompt = prompt.Replace($"[{key}]", value ?? string.Empty);
        }

        return prompt;
    }

    private static Dictionary<string, string> GetObjectProperties(object obj)
    {
        return obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                p => p.Name,
                p => p.GetValue(obj)?.ToString() ?? string.Empty
            );
    }
    
    public abstract Task<TResponse> SendRequestAsync(TRequest request, CancellationToken? cancellationToken = null);
    
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;
        
        if (disposing)
        {
            _initializationLock.Dispose();
        }

        IsDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AgentBase<TRequest, TResponse>));
    }
}
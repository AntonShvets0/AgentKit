using AgentKit.Models;

namespace AgentKit.Interfaces;

public interface IInferenceClient
{
    public IConversationContext Context { get; set; }
    public Task<string> CompleteChatAsync(
        
        string message,
        InferenceChatCompletionOptions options,
        
        IInferenceTool[] tools,
        IAgent source
        
        );
    
    public Task<T?> CompleteChatAsync<T>(
        
        string message,
        InferenceChatCompletionOptions options,
        
        IInferenceTool[] tools,
        IAgent source
        
    )
        where T : class;
}
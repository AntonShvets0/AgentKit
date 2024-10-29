using AgentKit.Models;

namespace AgentKit.Interfaces;

public interface IInferenceFactory
{
    public IInferenceClient CreateClient(
        
        InferenceType inferenceType,
        IConversationContext conversationContext
        
        );
}
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services;

public class MemoryPromptProvider : IPromptProvider
{
    private Dictionary<string, PromptTemplate> _prompts;

    public MemoryPromptProvider(Dictionary<string, PromptTemplate> prompts)
    {
        _prompts = prompts;
    }
    
    public ValueTask<PromptTemplate> GetPromptAsync(string promptId)
    {
        if (!_prompts.TryGetValue(promptId, out var prompt))
            throw new KeyNotFoundException($"{prompt} not exists");

        return new ValueTask<PromptTemplate>(prompt);
    }
}
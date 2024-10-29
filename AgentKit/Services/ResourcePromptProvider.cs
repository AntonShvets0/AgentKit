using System.Reflection;
using System.Resources;
using System.Text.Json;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services;

public class ResourcePromptProvider : IPromptProvider
{
    private readonly Dictionary<string, PromptTemplate> _prompts = new();
    private readonly Assembly _assembly;
    private readonly string _resourceName;
    
    public ResourcePromptProvider(
        string resourceName,
        Assembly assembly
        )
    {
        _resourceName = resourceName;
        _assembly = assembly;
    }
    
    
    public ValueTask<PromptTemplate> GetPromptAsync(string promptId)
    {
        if (_prompts.TryGetValue(promptId, out var promptTemplate))
            return new ValueTask<PromptTemplate>(promptTemplate);

        var resourceManager = new ResourceManager(_resourceName, _assembly);
        var resourceData = resourceManager.GetString(promptId);
        if (resourceData == null)
            throw new InvalidOperationException();

        promptTemplate = new PromptTemplate
        {
            Content = resourceData + ".txt"
        };

        _prompts[promptId] = promptTemplate ?? throw new InvalidOperationException(); 
        return new ValueTask<PromptTemplate>(promptTemplate);
    }
}
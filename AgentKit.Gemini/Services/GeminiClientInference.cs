using AgentKit.Gemini.Models;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.OpenAI.Models;
using AgentKit.OpenAI.Services;
using AgentKit.Services.Tools;
using Microsoft.Extensions.Options;

namespace AgentKit.Gemini.Services;

public abstract class GeminiClientInference : OpenAIClientInference
{
    public GeminiClientInference(
        ToolCompilerService toolCompilerService,
        IOptions<GeminiConfiguration> configuration) : base(toolCompilerService, new OpenAIConfiguration
    {
        Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
        Token = configuration.Value.Token
    })
    {
        
    }
    
    public GeminiClientInference(
        ToolCompilerService toolCompilerService,
        GeminiConfiguration configuration) : base(toolCompilerService, new OpenAIConfiguration
    {
        Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
        Token = configuration.Token
    })
    {
        
    }
    
    protected override bool AllowedJsonSchema { get; } = false;

    public override async Task<string> CompleteChatAsync(string message, InferenceChatCompletionOptions options, CompiledTool[] tools, IAgent source,
        Type? jsonMode)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                var text = await base.CompleteChatAsync(message, options, tools, source, jsonMode);

                return text.Replace("```json", "").Replace("```", "");
            }
            catch
            {
                retryCount++;
                await Task.Delay(1000);
                if (retryCount > 5) throw;
            }
        }
    }
}
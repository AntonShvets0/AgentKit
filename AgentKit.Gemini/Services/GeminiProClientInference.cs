using AgentKit.Gemini.Models;
using AgentKit.OpenAI.Models;
using AgentKit.Services.Tools;
using Microsoft.Extensions.Options;

namespace AgentKit.Gemini.Services;

public class GeminiProClientInference
    : GeminiClientInference
{
    public GeminiProClientInference(ToolCompilerService toolCompilerService, IOptions<GeminiConfiguration> configuration) : base(toolCompilerService, configuration) {}
    public GeminiProClientInference(ToolCompilerService toolCompilerService, GeminiConfiguration geminiConfiguration, GeminiConfiguration geminiConfiguration2) : base(toolCompilerService, geminiConfiguration) {}
    
    public override string Model { get; protected set; } = "gemini-2.0-pro-exp-02-05";
}
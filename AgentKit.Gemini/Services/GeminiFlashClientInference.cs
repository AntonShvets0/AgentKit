using AgentKit.Gemini.Models;
using AgentKit.Services.Tools;
using Microsoft.Extensions.Options;

namespace AgentKit.Gemini.Services;

public class GeminiFlashClientInference(ToolCompilerService toolCompilerService, IOptions<GeminiConfiguration> configuration)
    : GeminiClientInference(toolCompilerService, configuration)
{
    public override string Model { get; protected set; } = "gemini-2.0-flash";
}
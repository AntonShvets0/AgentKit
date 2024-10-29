using System.ComponentModel;

namespace AgentKit.OpenAI.Models;

public enum OpenAIModel
{
    [Description("gpt-4o")]
    GPT4o,
    
    [Description("gpt-4o-mini")]
    GPT4oMini
}
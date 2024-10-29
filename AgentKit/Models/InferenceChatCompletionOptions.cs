namespace AgentKit.Models;

public record InferenceChatCompletionOptions(
    float Temperature = 0.7f,
    bool IsSaveToHistory = true);
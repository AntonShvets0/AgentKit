using AgentKit.Models;

namespace AgentKit.Interfaces;

public interface IPromptProvider
{
    ValueTask<PromptTemplate> GetPromptAsync(string promptId);
}
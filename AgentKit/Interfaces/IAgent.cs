using AgentKit.Models;

namespace AgentKit.Interfaces;

public interface IAgent
{
    public AgentState State { get; }
    public Task<object> SendObjectRequest(object request, CancellationToken? cancellationToken = null);
}
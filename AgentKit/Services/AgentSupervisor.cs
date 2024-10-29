using AgentKit.Abstractions;
using AgentKit.Interfaces;

namespace AgentKit.Services;

public class AgentSupervisor<TRequest> : AgentBase<TRequest, object>, IAgentSupervisor
{
    protected List<IAgent> _agents = new();
    public AgentSupervisor(IInferenceFactory inferenceFactory, IPromptProvider provider) : base(inferenceFactory, provider) {}

    public void SubscribeAgent(IAgent agent)
        => _agents.Add(agent);

    public void UnsubscribeAgent(IAgent agent)
        => _agents.Remove(agent);

    public void UnsubscribeAgent(Predicate<IAgent> predicate) => _agents.RemoveAll(predicate);

    public override Task<object> SendRequestAsync(TRequest request, CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }
}
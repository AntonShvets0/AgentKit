namespace AgentKit.Interfaces;

/// <summary>
/// Перенаправляет запрос к нужному Agent
/// </summary>
public interface IAgentSupervisor : IAgent
{
    void SubscribeAgent(IAgent agent);
    void UnsubscribeAgent(IAgent agent);
    void UnsubscribeAgent(Predicate<IAgent> agent);
}
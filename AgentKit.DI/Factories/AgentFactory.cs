using AgentKit.Interfaces;

namespace AgentKit.DI.Factories;

public class AgentFactory
{
    private readonly List<IAgent> _agents;
    public AgentFactory(IEnumerable<IAgent> agents)
    {
        _agents = agents.ToList();
    }
    
    // TODO: Реализовать пул агентов
    public T RentAgent<T>()
        where T : IAgent
    {
        return _agents
            .OfType<T>()
            .First();
    }
}
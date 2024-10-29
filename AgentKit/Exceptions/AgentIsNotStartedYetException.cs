namespace AgentKit.Exceptions;

public class AgentIsNotStartedYetException : Exception
{
    public AgentIsNotStartedYetException(string message) : base(message) {}
}
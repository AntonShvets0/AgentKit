namespace AgentKit.Models;

public class ProactiveResponse<T>
    where T : class
{
    public string Event { get; set; }
    public T Response { get; set; }
}
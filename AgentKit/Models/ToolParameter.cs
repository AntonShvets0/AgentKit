namespace AgentKit.Models;

public class ToolParameter
{
    public string Type { get; set; }
    public string Description { get; set; }
    public string[]? Enum { get; set; }
    public Dictionary<string, ToolParameter>? Properties { get; set; }
    public string[] Required { get; set; }
}
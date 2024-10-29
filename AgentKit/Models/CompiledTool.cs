using System.Text.Json.Nodes;
using AgentKit.Interfaces;

namespace AgentKit.Models;

public class CompiledTool
{
    public string Name { get; set; }
    public ToolParameter Parameters { get; set; }
    
    public IInferenceTool Tool { get; set; }
}
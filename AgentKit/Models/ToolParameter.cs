using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentKit.Models;

public class ToolParameter
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("enum")]
    public string[] Enum { get; set; }
    
    [JsonPropertyName("properties")]
    public Dictionary<string, ToolParameter> Properties { get; set; }
    
    [JsonPropertyName("required")]
    public string[] Required { get; set; }
    
    [JsonPropertyName("items")]
    public ToolParameter Items { get; set; }
}
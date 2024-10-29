using AgentKit.Interfaces;

namespace AgentKit.Exceptions;

public class CompilerToolException(IInferenceTool tool, string error) : Exception($"An error occurred while compiling {tool.GetType().Name}: {error}")
{
    
}
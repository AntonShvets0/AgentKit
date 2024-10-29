using System.Reflection;
using AgentKit.Attributes;
using AgentKit.Exceptions;
using AgentKit.Interfaces;

namespace AgentKit.Services.Tools;

public class ToolMethodRoutingService
{
    public MethodInfo FindToolMethod(IInferenceTool tool)
    {
        var invokeMethods = tool.GetType().GetMethods().Where(m => 
            !m.IsStatic &&
            !m.IsAbstract &&
            m.IsPublic &&
            m.GetCustomAttribute<ToolAttribute>() != null).ToList();
            
        if (invokeMethods.Count > 1)
            throw new CompilerToolException(tool, "Compiler cannot determine which method to call (more than one method with the ToolAttribute attribute)");
        if (!invokeMethods.Any())
            throw new CompilerToolException(tool, "Method that LLM should call was not found. Use ToolAttribute");

        return invokeMethods.First();
    }
}
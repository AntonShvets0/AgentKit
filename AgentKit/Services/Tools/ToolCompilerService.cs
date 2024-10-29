using System.Text.Json.Nodes;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services.Tools;

public class ToolCompilerService
{
    public ToolParameterCompilerService Parameter { get; }
    public ToolMethodInvokerService MethodInvoker { get; }
    public ToolMethodRoutingService MethodFinder { get; }
    

    public ToolCompilerService(
        ToolMethodInvokerService methodInvoker,
        ToolParameterCompilerService parameter,
        ToolMethodRoutingService methodFinder)
    {
        MethodInvoker = methodInvoker;
        Parameter = parameter;
        MethodFinder = methodFinder;
    }

    public CompiledTool Compile(IInferenceTool tool)
    {
        var method = MethodFinder.FindToolMethod(tool);
        var parameters = Parameter.CompileParameters(method);
        
        var result = new CompiledTool
        {
            Name = tool.GetType().Name.Replace("InferenceTool", ""),
            Parameters = parameters,
            Tool = tool
        };

        return result;
    }

    public async Task<string> CallMethodAsync(
        IInferenceTool tool, 
        JsonObject jsonParams, 
        IInferenceClient? inferenceClient = null,
        IAgent? agent = null)
    {
        var method = MethodFinder.FindToolMethod(tool);
        var args = Parameter.PrepareArguments(method, jsonParams, tool, inferenceClient, agent);
        var result = await MethodInvoker.InvokeMethodAsync(method, tool, args);
        return MethodInvoker.SerializeResult(result);
    }
}
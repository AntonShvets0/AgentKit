using System.Reflection;
using System.Text.Json;
using AgentKit.Interfaces;

namespace AgentKit.Services.Tools;

public class ToolMethodInvokerService
{
    private bool IsAsyncMethod(MethodInfo method)
    {
        return typeof(Task).IsAssignableFrom(method.ReturnType);
    }

    public async Task<object?> InvokeMethodAsync(MethodInfo method, IInferenceTool tool, object?[] args)
    {
        if (IsAsyncMethod(method))
        {
            var task = (Task)method.Invoke(tool, args)!;
            await task.ConfigureAwait(false);

            if (method.ReturnType == typeof(Task))
                return null;

            var resultProperty = method.ReturnType.GetProperty("Result")!;
            return resultProperty.GetValue(task);
        }

        return method.Invoke(tool, args);
    }

    public string SerializeResult(object? result)
    {
        if (result == null)
            return "null";

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }   
}
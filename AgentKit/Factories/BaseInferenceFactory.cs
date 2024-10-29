using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Services.Tools;

namespace AgentKit.Factories;

/// <summary>
/// Стоит использовать в приложениях без ASP.NET.
/// В остальном случае, используйте InferenceFactory из AgentKit.DI
/// </summary>
public class BaseInferenceFactory : IInferenceFactory
{
    private Dictionary<InferenceType, IInferenceClient> _clients;
    private BaseInferenceFactory() {}
    
    public IInferenceClient CreateClient(InferenceType inferenceType, IConversationContext conversationContext)
    {
        if (_clients.TryGetValue(inferenceType, out var inferenceClient))
        {
            inferenceClient.Context = conversationContext;
            return inferenceClient;
        }

        var client = _clients.MinBy(c =>
            Math.Abs((int)c.Key - (int)inferenceType)); // Вычисляем наиболее близкую модель 
        
        client.Value.Context = conversationContext;
        return client.Value;
    }

    private static object GetService(Type t, 
        ToolCompilerService toolCompilerService,
        object[] configurations)
    {
        var configuration = 
            configurations.FirstOrDefault(c => t == c.GetType());
        if (configuration != null)
            return configuration;

        if (t == typeof(ToolCompilerService))
            return toolCompilerService;

        throw new ArgumentException(t.Name);
    }
    
    public static BaseInferenceFactory Get(
        ToolCompilerService toolCompilerService,
        object[] configurations,
        params (InferenceType, Type)[] inferences
        )
    {
        var clients = new Dictionary<InferenceType, IInferenceClient>();
        foreach (var (inferenceType, inference) in inferences)
        {
            if (!inference.IsAssignableTo(typeof(IInferenceClient)))
                throw new InvalidOperationException("Inference must be assignable to IInferenceClient");

            var constructor = inference.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = constructor.GetParameters()
                .Select(param => GetService(param.ParameterType, toolCompilerService, configurations))
                .ToArray();

            clients[inferenceType] = (IInferenceClient)Activator.CreateInstance(inference, parameters);
        }

        return new BaseInferenceFactory
        {
            _clients = clients
        };
    }

    public static BaseInferenceFactory Get(
        object configuration,
        params (InferenceType, Type)[] inferences
    )
    {
        return Get(
            new ToolCompilerService(
                new ToolMethodInvokerService(),
                new ToolParameterCompilerService(),
                new ToolMethodRoutingService()
            ),
            [configuration],
            inferences
            );
    }
}
using AgentKit.DI.Factories;
using AgentKit.DI.Models;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Services.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace AgentKit.DI.Extensions;

public static class AgentExtension
{
    public static IServiceCollection AddAgentKit(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ToolCompilerService>();
        serviceCollection.AddSingleton<ToolMethodInvokerService>();
        serviceCollection.AddSingleton<ToolParameterCompilerService>();
        serviceCollection.AddSingleton<JsonValueConverter>();
        
        serviceCollection.AddScoped<InferenceFactory>();
        serviceCollection.AddScoped<AgentFactory>();
        
        return serviceCollection;
    }

    public static IServiceCollection AddAgent<T>(this IServiceCollection serviceCollection)
        where T : class, IAgent
    {
        serviceCollection.AddScoped<T>();
        return serviceCollection;
    }

    public static IServiceCollection AddInference<T>(this IServiceCollection serviceCollection, InferenceType inferenceType)
        where T : class, IInferenceClient
    {
        serviceCollection.AddTransient<T>();
        serviceCollection.AddTransient<InferenceClientContext>(factory
            => new InferenceClientContext(
                
                factory.GetRequiredService<T>(),
                inferenceType
                
                ));

        return serviceCollection;
    }
}
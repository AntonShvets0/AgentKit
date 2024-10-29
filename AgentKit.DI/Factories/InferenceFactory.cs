using AgentKit.DI.Models;
using AgentKit.Interfaces;
using AgentKit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AgentKit.DI.Factories;

public class InferenceFactory : IInferenceFactory
{
    private IServiceProvider _service;
    public InferenceFactory(IServiceProvider service)
    {
        _service = service;
    }

    public IInferenceClient CreateClient(InferenceType inferenceType, IConversationContext conversationContext)
    {
        var clients = _service.GetServices<InferenceClientContext>().ToList();
        var client = clients.FirstOrDefault(c => c.InferenceType == inferenceType);
        if (client != null)
        {
            client.InferenceClient.Context = conversationContext;
            return client.InferenceClient;
        }

        client = clients.MinBy(c => Math.Abs((int)c.InferenceType - (int)inferenceType));
        if (client?.InferenceClient == null) throw new InvalidOperationException();
        client.InferenceClient.Context = conversationContext;
        
        return client.InferenceClient;
    }
}
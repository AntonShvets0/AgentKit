using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.DI.Models;

public class InferenceClientContext
{
    public IInferenceClient InferenceClient { get; set; }
    public InferenceType InferenceType { get; set; }

    public InferenceClientContext(
        IInferenceClient inferenceClient,
        InferenceType inferenceType
    )
    {
        InferenceClient = inferenceClient;
        InferenceType = inferenceType;
    }
}
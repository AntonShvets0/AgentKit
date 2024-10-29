using System.ClientModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AgentKit.Abstractions;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Models.Chat;
using AgentKit.Models.Chat.MessageAttachments;
using AgentKit.OpenAI.Models;
using AgentKit.Services.Tools;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace AgentKit.OpenAI.Services;

public abstract class OpenAIClientInference(
    ToolCompilerService toolCompilerService,
    OpenAIConfiguration configuration)
    : InferenceBase<OpenAIConfiguration>(toolCompilerService, configuration)
{
    protected readonly ToolCompilerService _toolCompilerService = toolCompilerService;
    public abstract OpenAIModel Model { get; protected set; }
    
    private OpenAIClient _client = new(new ApiKeyCredential(configuration.Token), new OpenAIClientOptions
    {
        Endpoint = new Uri(configuration.Endpoint ?? "https://api.openai.com")
    });

    public override async Task<T?> CompleteChatAsync<T>(string message, InferenceChatCompletionOptions options, CompiledTool[] tools, IAgent source)
        where T : class
        => JsonSerializer.Deserialize<T>(await CompleteChatAsync(message, options, tools, source, typeof(T)));

    public override Task<string> CompleteChatAsync(string message, InferenceChatCompletionOptions options, CompiledTool[] tools, IAgent source)
        => CompleteChatAsync(message, options, tools, source, null);

    public async Task<string> CompleteChatAsync(
        string message, 
        InferenceChatCompletionOptions options, 
        CompiledTool[] tools, 
        IAgent source,
        Type? jsonMode
        )
    {
        var chatClient =
            _client.GetChatClient(
                Model.GetType().GetCustomAttribute<DescriptionAttribute>()?.Description ?? 
                throw new ArgumentNullException(nameof(Model)));
        var convertedChatMessages = MergeChatMessages(Context.GetHistory())
            .Select(c =>
            {
                var chatContent = c.Content.Select(c2 =>
                    c2 is TextAttachment textAttachment
                        ? ChatMessageContentPart.CreateTextPart(textAttachment.Content)
                        : (c2 is PhotoAttachment photoAttachment
                            ? ChatMessageContentPart.CreateImagePart(new Uri(photoAttachment.Url))
                            : null)
                ).ToArray();

                return (ChatMessage)(c.Role switch
                {
                    ChatRole.Assistant => new AssistantChatMessage(chatContent),
                    ChatRole.User => new UserChatMessage(chatContent),
                    ChatRole.System => new SystemChatMessage(chatContent),
                    _ => throw new InvalidOperationException()
                });
            }).ToList();

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = options.Temperature,
            ResponseFormat = jsonMode == null
                ? ChatResponseFormat.CreateTextFormat()
                : ChatResponseFormat.CreateJsonSchemaFormat(
                    "schema",
                    new BinaryData(
                        _toolCompilerService.Parameter.SchemaGenerator.SerializeJsonSchema(
                            _toolCompilerService.Parameter.SchemaGenerator.GenerateSchema(jsonMode))),
                    jsonSchemaIsStrict: true
                ),
        };
        foreach (var compiledTool in tools)
        {
            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(compiledTool.Name, compiledTool.Tool.Description, 
                new BinaryData(_toolCompilerService.Parameter.SchemaGenerator.SerializeJsonSchema(compiledTool.Parameters)),
                functionSchemaIsStrict: true));
        }
        
        
        var response = await chatClient.CompleteChatAsync(convertedChatMessages, chatOptions);
        var handledMessage = await CallToolsAsync(response.Value, source, convertedChatMessages, tools, options);
        convertedChatMessages.Add(handledMessage);

        if (options.IsSaveToHistory)
        {
            Context.ChatMessages.Add(new AgentKit.Models.Chat.ChatMessage
            {
                Content = [new TextAttachment { Content = message }],
                Role = ChatRole.User
            });
            
            Context.ChatMessages.Add(new AgentKit.Models.Chat.ChatMessage
            {
                Content = [new TextAttachment { Content = handledMessage.Content[0].Text }],
                Role = ChatRole.Assistant
            });
        }

        return handledMessage.Content[0].Text;
    }
    
    protected async Task<AssistantChatMessage> CallToolsAsync(
        ChatCompletion response,
        IAgent source,
        List<ChatMessage> chatMessages,
        CompiledTool[] tools,
        InferenceChatCompletionOptions inferenceChatCompletionOptions
        
        )
    {
        if (response.ToolCalls.Count < 1)
        {
            return new AssistantChatMessage(response);
        }
        
        foreach (var toolCall in response.ToolCalls)
        {
            var inferenceTool =
                tools.FirstOrDefault(t => 
                    t.Tool.GetType().Name.Replace("InferenceToo", "") == toolCall.FunctionName);
            if (inferenceTool == null)
                continue; // Возможно стоит добавить исключение? Не уверен, так как LLM достаточно хаотичны

            var toolResponse =
                await toolCompilerService.CallMethodAsync(
                    inferenceTool.Tool, 
                    JsonDocument.Parse(toolCall.FunctionArguments.ToString()).Deserialize<JsonObject>(),
                    this,
                    source
                    );
            chatMessages.Add(new ToolChatMessage(toolCall.Id, toolResponse));
        }
        
        var chatClient =
            _client.GetChatClient(
                Model.GetType().GetCustomAttribute<DescriptionAttribute>()?.Description ?? 
                throw new ArgumentNullException(nameof(Model)));

        var result = await chatClient.CompleteChatAsync(chatMessages, new ChatCompletionOptions
        {
            Temperature = inferenceChatCompletionOptions.Temperature
        });
        return await CallToolsAsync(result.Value, source, chatMessages, tools, inferenceChatCompletionOptions);
    }
}
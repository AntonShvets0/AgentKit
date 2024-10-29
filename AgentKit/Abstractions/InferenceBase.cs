using System.Text;
using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Models.Chat;
using AgentKit.Models.Chat.MessageAttachments;
using AgentKit.Services.Context;
using AgentKit.Services.Tools;

namespace AgentKit.Abstractions;

public abstract class InferenceBase<TConfiguration> : IInferenceClient
{
    public IConversationContext Context { get; set; }
    protected TConfiguration Configuration { get; }
    protected ToolCompilerService ToolCompilerService { get; }
    
    protected InferenceBase(
        
        ToolCompilerService toolCompilerService,
        TConfiguration configuration
        
        )
    {
        Context = new LongConversationContext(null, []);
        ToolCompilerService = toolCompilerService;
        Configuration = configuration;
    }

    
    public async Task<string> CompleteChatAsync(string message, InferenceChatCompletionOptions options,
        IInferenceTool[] tools, IAgent source)
    {
        var compiledTools = tools.Select(ToolCompilerService.Compile).ToArray();

        var response = await CompleteChatAsync(message, options, compiledTools, source);
        if (this is IInferenceSummarizer inferenceSummarizer && 
            Context.ChatMessages.Count > inferenceSummarizer.MaxContextLength)
        {
            await SummarizeAsync(source);
        }

        return response;
    }

    public async Task<T?> CompleteChatAsync<T>(string message, InferenceChatCompletionOptions options,
        IInferenceTool[] tools, IAgent source)
        where T : class
    {
        var compiledTools = tools.Select(ToolCompilerService.Compile).ToArray();

        var response = await CompleteChatAsync<T>(message, options, compiledTools, source);
        if (this is IInferenceSummarizer inferenceSummarizer && 
            Context.ChatMessages.Count > inferenceSummarizer.MaxContextLength)
        {
            await SummarizeAsync(source);
        }

        return response;
    }

    public abstract Task<string> CompleteChatAsync(string message, InferenceChatCompletionOptions options,
        CompiledTool[] tools, IAgent source);

    public abstract Task<T?> CompleteChatAsync<T>(string message, InferenceChatCompletionOptions options,
        CompiledTool[] tools, IAgent source)
        where T : class;

    protected virtual async Task<string> SummarizeAsync(IAgent source)
    {
        if (this is not IInferenceSummarizer inferenceSummarizer)
            throw new InvalidOperationException("Summarize method can be invoked only with IInferenceSummarizer");
        
        var summarizedText = await CompleteChatAsync(
            "[Now pause the chat with me and create a short recap of our chat. Keep important parts, remove water and unnecessary parts.]\n" +
            "[Just write a short summary, don't talk to me]:", 
            new InferenceChatCompletionOptions(
                0.3f, 
                IsSaveToHistory: false),
            Array.Empty<CompiledTool>(), source);
        
        Context.ChatMessages = Context.ChatMessages.TakeLast(inferenceSummarizer.MaxContextLength / 2).ToList();
        await inferenceSummarizer.InvokeSummarizingEvent(summarizedText);

        Context.ChatMessages.RemoveAll(c => c.Tag == "Summarized");
        Context.ChatMessages.Insert(0, new ChatMessage
        {
            Role = ChatRole.System,
            Content =
            [
                new TextAttachment { Content = $"Brief summary of the chat: {summarizedText}" }
            ],
            Tag = "Summarized"
        });

        return summarizedText;
    }
    
    protected ChatMessage[] MergeChatMessages(ChatMessage[] messages)
    {
        if (messages == null || messages.Length <= 1)
            return messages;

        var result = new List<ChatMessage>();
        var currentRole = messages[0].Role;
        var currentTag = messages[0].Tag;
        var currentAttachments = new List<IChatAttachment>(messages[0].Content);

        for (int i = 1; i < messages.Length; i++)
        {
            if (messages[i].Role == currentRole && messages[i].Tag == currentTag)
            {
                foreach (var attachment in messages[i].Content)
                {
                    if (attachment is TextAttachment textAttachment)
                    {
                        var newTextAttachment = new TextAttachment
                        {
                            Content = "\n" + textAttachment.Content
                        };
                        currentAttachments.Add(newTextAttachment);
                    }
                    else
                    {
                        currentAttachments.Add(attachment);
                    }
                }
            }
            else
            {
                result.Add(new ChatMessage
                {
                    Role = currentRole,
                    Tag = currentTag,
                    Content = currentAttachments.ToArray()
                });

                currentRole = messages[i].Role;
                currentTag = messages[i].Tag;
                currentAttachments = new List<IChatAttachment>(messages[i].Content);
            }
        }

        // Добавляем последнее накопленное сообщение
        result.Add(new ChatMessage
        {
            Role = currentRole,
            Tag = currentTag,
            Content = currentAttachments.ToArray()
        });

        return result.ToArray();
    }
}
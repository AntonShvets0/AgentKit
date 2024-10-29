using AgentKit.Interfaces;
using AgentKit.Models;
using AgentKit.Models.Chat;
using AgentKit.Models.Chat.MessageAttachments;

namespace AgentKit.Services.Context;

/// <summary>
/// Контекст, который предоставляет нейросети полный чат. Более дорогой, но качественный вариант
/// </summary>
public class LongConversationContext : IConversationContext
{
    public List<ChatMessage> ChatMessages { get; set; }

    public LongConversationContext(string? prompt, List<ChatMessage> chatMessages)
    {
        if (prompt != null) chatMessages.Insert(0, new ChatMessage
        {
            Role = ChatRole.System,
            Content = [new TextAttachment { Content = prompt }]
        });
        
        ChatMessages = chatMessages;
    }

    public void InsertToHistory(ChatMessage[] chatMessages)
        => ChatMessages.AddRange(chatMessages);

    public ChatMessage[] GetHistory()
        => ChatMessages.ToArray();
}
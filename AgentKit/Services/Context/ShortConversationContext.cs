using AgentKit.Interfaces;
using AgentKit.Models.Chat;
using AgentKit.Models.Chat.MessageAttachments;

namespace AgentKit.Services.Context;

/// <summary>
/// Контекст, который предоставляет частичный доступ к чату, в зависимости от глубины
/// </summary>
public class ShortConversationContext : IConversationContext
{
    public List<ChatMessage> ChatMessages { get; set; }
    
    /// <summary>
    /// Глубина сообщений, которые будут предоставляться нейросети. Если указана 10 - то нейросеть получит последние 10 сообщений
    /// </summary>
    public int Depth { get; set; }

    public ShortConversationContext(string? prompt, List<ChatMessage>? chatMessages, int depth = 10)
    {
        if (prompt != null) chatMessages.Insert(0, new ChatMessage
        {
            Role = ChatRole.System,
            Content = [new TextAttachment { Content = prompt }]
        });
        
        ChatMessages = chatMessages;
        Depth = depth;
    }

    public void InsertToHistory(ChatMessage[] chatMessages)
        => ChatMessages.AddRange(chatMessages);

    public virtual ChatMessage[] GetHistory()
        => ChatMessages.TakeLast(Depth).ToArray();
}
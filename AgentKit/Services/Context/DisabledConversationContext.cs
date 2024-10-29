using AgentKit.Interfaces;
using AgentKit.Models.Chat;

namespace AgentKit.Services.Context;

/// <summary>
/// Указывает, что у запросов нет контекста
/// </summary>
public class DisabledConversationContext : IConversationContext
{
    public List<ChatMessage> ChatMessages { get; set; } = new();

    public void InsertToHistory(ChatMessage[] chatMessages) {}
    public virtual ChatMessage[] GetHistory()
        => [];
}
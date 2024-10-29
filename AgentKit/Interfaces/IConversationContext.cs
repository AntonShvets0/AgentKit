using AgentKit.Models.Chat;

namespace AgentKit.Interfaces;

public interface IConversationContext
{
    public List<ChatMessage> ChatMessages { get; set; }
    public ChatMessage[] GetHistory();
    public void InsertToHistory(ChatMessage[] chatMessages);
}
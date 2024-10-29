using AgentKit.Models.Chat.MessageAttachments;

namespace AgentKit.Models.Chat;

public struct ChatMessage
{
    public IChatAttachment[] Content { get; set; }
    public ChatRole Role { get; set; }
    
    public string Tag { get; set; }
}
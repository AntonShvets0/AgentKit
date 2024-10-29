namespace AgentKit.Models.Chat;

public enum ChatRole
{
    System,
    Assistant,
    User,
    
    // Остальные роли, такие как Tool не добавлены, так как их логика инкапсулирована внутри IInferenceClient.
    // Клиент работает только с готовыми данными
}
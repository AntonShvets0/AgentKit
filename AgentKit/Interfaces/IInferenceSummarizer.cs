namespace AgentKit.Interfaces;

/// <summary>
/// Если агент реализует этот интерфейс, то он подразумевает суммаризацию раз в определенное количество сообщений
/// </summary>
public interface IInferenceSummarizer
{
    /// <summary>
    /// Через сколько сообщений происходит суммаризация
    /// </summary>
    public int MaxContextLength { get; }

    /// <summary>
    /// Обязательно нужно подписаться на этот event, чтобы ловить суммаризацию. Ее необходимо записать в базу данных, например.
    /// </summary>
    public event Func<string, Task> OnSummarizingEvent;

    public Task InvokeSummarizingEvent(string summarizedText);
}
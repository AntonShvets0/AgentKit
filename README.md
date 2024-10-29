# AgentKit

Данная библиотека в разработке и может быть нестабильной!
Библиотека для создания автономных LLM агентов на платформе .NET.

## Установка

Для использования DI необходимо установить библиотеку AgentKit.DI.

## Начало работы

### Регистрация в DI ASP.NET

```csharp
// Добавляем инференс LLM в DI контейнер. Реализована поддержка GPT через AgentKit.OpenAI
// В библиотеке AgentKit мы ввели понятие InferenceType (Big, Middle, Small). 
// Это позволяет агентам, без обращения к определенным моделям, задавать уровень мощности, необходимый для запросов
// Например, агенту нужна модель для простой задачи. Он вызывает InferenceType.Small 
// В DI контейнере такой тип имеет модель GPT4o-mini, соответственно будет вызвана она.

builder.Services.AddInference<Gpt4oClientInference>(InferenceType.Big);
builder.Services.AddInference<Gpt4oMiniClientInference>(InferenceType.Small);

// Конфигурация для OpenAI моделей
builder.Services.Configure<OpenAIConfiguration>(builder.Configuration.GetSection("OpenAI"));

// Регистрация основного функционала
builder.Services.AddAgentKit();

// Регистрация агента
builder.Services.AddAgent<ModeratorAgent>();
```

## Агенты

Агенты - это автономные программные системы на базе больших языковых моделей, способные анализировать задачи, планировать и выполнять действия через взаимодействие с внешними инструментами и API для достижения поставленных целей.

В контексте AgentKit, агенты представляют собой набор определенного поведения для LLM, который инкапсулирует сложную логику.

### Создание простого агента

```csharp
/// <summary>
/// Простейший агент для модерации сообщений
/// </summary>
public class ModeratorAgent(
    IInferenceFactory inferenceFactory,
    int severity = 75
    )
    : AgentBase<ModeratorRequest, ModeratorVerdict>(
        inferenceFactory, 
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
{
    private readonly int _severity = severity;
    
    public override async Task<ModeratorVerdict> SendRequestAsync(ModeratorRequest request, CancellationToken? cancellationToken = null)
    {
        var inference = InferenceFactory.CreateClient(
            InferenceType.Middle,
            new DisabledConversationContext()
        );

        var response = await inference.CompleteChatAsync<ModeratorResponse>(
            await GetPromptAsync("moderate", new { text = request.Text }),
            new InferenceChatCompletionOptions(),
            [],
            this
        );
        
        if (response == null) 
            return new ModeratorVerdict(true);

        return new ModeratorVerdict(
            response.Ad >= _severity ||
            response.Nsfw >= _severity ||
            response.HateSpeech >= _severity
        );
    }
}
```

### Использование агента

```csharp
private AgentFactory _agentFactory;

public async Task ModerateAsync(string message)
{
    var agent = _agentFactory.RentAgent<ModeratorAgent>(50); // 50 - строгость агента
    var response = await agent.SendRequestAsync(new ModeratorRequest(message));
    
    if (response.IsHarmulContent) 
    {
        // Обработка вредоносного контента    
    }
}
```

## Виды Агентов

### AgentBase
Базовый агент, возвращающий объект в ответ на объект.

### HostedAgent
Фоновый агент, который:
- Не может выполнить запрос напрямую
- Работает в фоне, периодически вызывая запросы к LLM
- Перед каждым вызовом проверяет необходимость вызова модели
- Сохраняет результаты в Queue Events

Пример:
```csharp
/// <summary>
/// Автономный агент для анализа логов и регистрации аномалий
/// </summary>
public class LogAnalyserAgent(IInferenceFactory inferenceFactory)
    : HostedAgent<LogAnalyserWarning>(inferenceFactory, 
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
{
    protected override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    public override ValueTask<bool> CanExecuteAsync(int iteration)
    {
        return new ValueTask<bool>(true);
    }

    public override async Task<LogAnalyserWarning?> SendRequestAsync(HostedRequest request, CancellationToken? cancellationToken = null)
    {
        var log = ""; // Получение лога из источника
        
        var inference = InferenceFactory.CreateClient(
            InferenceType.Big,
            new DisabledConversationContext()
        );

        var response = await inference.CompleteChatAsync<LogAnalyser>(
            await GetPromptAsync("logAnalyse", new { log = log }),
            new InferenceChatCompletionOptions(Temperature: 0.1f),
            [],
            this
        );
        
        if (response == null)
            throw new HostedException();

        if (response.CriticalLevel < 10)
            throw new HostedException();

        return new LogAnalyserWarning
        {
            CriticalLevel = response.CriticalLevel,
            WarningReason = response.WarningReason,
            Log = log
        };
    }
}
```

### Запуск HostedAgent

```csharp
var analyser = _agentFactory.RentAgent<LogAnalyserAgent>();
await analyser.RunAsync();

// Через время
foreach (var @event in analyser.Events) {
    // Обработка логов
}
```

### ProactiveAgent

Наследует HostedAgent. Отличается тем, что позволяет подписываться на события вместо ручной проверки Event Log.

Пример:
```csharp
/// <summary>
/// Автономный агент для автоматического перевода новых постов
/// </summary>
public class AutoTranslatorAgent : ProactiveAgent<AutoTranslatorResponse>
{
    private string[] _languages;
    protected override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    public AutoTranslatorAgent(
        IInferenceFactory inferenceFactory, 
        Func<AutoTranslatorResponse, Task> onNewPost,
        string[] translatorLanguages
    ) : base(
        inferenceFactory,
        new ResourcePromptProvider("AgentKit.Example.Prompts", Assembly.GetExecutingAssembly()))
    {
        _languages = translatorLanguages;
        SubscribeEvent("onNewPost", onNewPost);
    }
    
    // Реализация методов...
}
```

### SupervisorAgent

Агент, который определяет, какого подписанного агента вызвать, и делегирует запрос ему.

```csharp
var supervisorAgent = _agentFactory.RentAgent<SupervisorAgent>();
supervisorAgent.SubscribeAgent(billingSupportAgent);
supervisorAgent.SubscribeAgent(technicalSupportAgent);

var response = await supervisorAgent.SendRequestAsync(
    new UserQuestionRequest("Здравствуйте! Как пополнить счет?")
);
```

## Tools

AgentKit предоставляет удобный способ создания инструментов:

```csharp
public class GetWeatherForecastInferenceTool : IInferenceTool
{
    public string Description { get; set; } = "This tool will allow you to get a weather forecast";

    [Tool]
    public object Execute(
        [Description("City")]
        string city,
        
        [Description("You can specify the country for greater accuracy")]
        string? country, // nullable параметры необязательны
        
        [Description("If this parameter is false, then you will get the weather forecast for tomorrow")]
        bool forCurrentDate = true // параметры со значением по умолчанию тоже необязательны
    )
    {
        return new { Temperature = 27 };
    }
}
```

Метод может быть асинхронным и синхронным. Он должен быть обозначен аттрибутом [Tool].
Чтобы LLM использовал этот метод, достаточно передать этот класс при вызове запроса в агенте:

```csharp
var response = await inference.CompleteChatAsync<ModeratorResponse>(
    ...,
    new InferenceChatCompletionOptions(),
    [new GetWeatherForecastInferenceTool()],
    this
);
```

И все! Если LLM вызовет этот метод, он вызовется в коде. Вам не нужно писать JSON схему.

CompleteChatAsync ВСЕГДА вернет текст. Он не будет возвращать промежуточные результаты, где LLM будет просить результат функции - он обработает это сам.

На основе параметров метода генерируется JSON схема для TOOL. Также поддерживаются любые классы, enum, массивы и коллекции в аргуметах.


## Контекст

AgentKit поддерживает 4 типа контекста:

1. **DisabledConversationContext**: Без истории диалога
```csharp
var inference = InferenceFactory.CreateClient(
    InferenceType.Big,
    new DisabledConversationContext()
);
```

2. **LongConversationContext**: Полная история диалога
```csharp
var inference = InferenceFactory.CreateClient(
    InferenceType.Big,
    new LongConversationContext("prompt", chatMessages)
);
```

3. **ShortConversationContext**: N последних сообщений
```csharp
var inference = InferenceFactory.CreateClient(
    InferenceType.Big,
    new ShortConversationContext("prompt", chatMessages, depth: 10)
);
```

4. **RagConversationContext**: N последних сообщений + релевантная информация
```csharp
var inference = InferenceFactory.CreateClient(
    InferenceType.Big,
    new RagConversationContext(
        "prompt", 
        chatMessages,
        new List<RagDocument>
        {
            new RagDocument("Важная информация", Language.Russian)
        },
        depth: 10,
        documentDepth: 2)
);
```

## Суммаризация

AgentKit поддерживает суммаризацию длинных диалогов в ShortConversationContext. Необходимо создать кастомный IInferenceClient и в нем реализовать интерфейс IInferenceSummarizer:

```csharp
public interface IInferenceSummarizer
{
    /// <summary>
    /// Интервал суммаризации (количество сообщений)
    /// </summary>
    public int MaxContextLength { get; }

    /// <summary>
    /// Event для сохранения суммаризации
    /// </summary>
    public event Func<string, Task> OnSummarizingEvent;

    public Task InvokeSummarizingEvent(string summarizedText);
}
```

## Промпты

AgentKit поддерживает несколько провайдеров промптов:

```csharp
// Из ресурсов проекта (рекомендуется)
var resources = new ResourcePromptProvider(
    "AgentKit.Example.Prompts", 
    Assembly.GetExecutingAssembly()
);

// Из файловой системы
var file = new FilePromptProvider("assets/prompts");

// Из памяти (для тестирования)
var memory = new MemoryPromptProvider(new Dictionary<string, PromptTemplate>() {
    {"animalPrompt", "Ты - [animal]"}
});
```

### Использование переменных в промптах

```csharp
var prompt = await GetPromptAsync("animalPrompt", new {
   animal = "кошка"
}); // результат: "Ты - кошка"
```
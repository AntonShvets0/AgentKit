using System.Reflection.Metadata;
using AgentKit.Interfaces;
using AgentKit.Models.Chat;
using System.Text.RegularExpressions;
using AgentKit.Models;
using AgentKit.Models.Chat.MessageAttachments;
using StopWords;

namespace AgentKit.Services.Context;

/// <summary>
/// Контекст, который предоставляет частичный доступ к чату, помимо этого добавляет к контексту RAG документы в зависимости от запроса.
/// TODO: Добавить лемматизацию
/// </summary>
public class RagConversationContext : ShortConversationContext
{
    private List<string>? _convertedRelevantDocuments;
    private readonly List<RagDocument> _documents;

    private const double RelevanceThreshold = 0.3;
    
    public int DocumentDepth { get; set; }

    public RagConversationContext(string? prompt, List<ChatMessage>? chatMessages, List<RagDocument> documents, int documentDepth = 2, int depth = 10) 
        : base(prompt, chatMessages, depth)
    {
        _documents = documents;
        DocumentDepth = documentDepth;
    }

    public override ChatMessage[] GetHistory()
    {
        var relevantDocs = GetRelevantDocuments();
        var docMessages = relevantDocs.Select(doc => new ChatMessage
        {
            Role = ChatRole.System,
            Content = [
                new TextAttachment { Content = $"Context information: {doc}" }
            ]
        }).ToList();

        var history = base.GetHistory().ToList();
        history.InsertRange(0, docMessages);
        
        return history.ToArray();
    }

    private List<string> GetRelevantDocuments()
    {
        if (_convertedRelevantDocuments == null)
        {
            _convertedRelevantDocuments = new();
            foreach (var document in _documents)
            {
                _convertedRelevantDocuments.Add(StopWord.RemoveStopWords(document.Content, document.Language));
            }
        }

        var lastMessages = ChatMessages
            .TakeLast(DocumentDepth)
            .Select(m => m.Content)
            .ToList();

        var keywords = ExtractKeywords(string.Join(" ", lastMessages));
        var relevantDocs = new List<(string doc, double relevance)>();

        foreach (var doc in _convertedRelevantDocuments)
        {
            var docKeywords = ExtractKeywords(doc);
            var relevance = CalculateRelevance(keywords, docKeywords);
            
            if (relevance >= RelevanceThreshold)
            {
                relevantDocs.Add((doc, relevance));
            }
        }

        return relevantDocs
            .OrderByDescending(x => x.relevance)
            .Select(x => x.doc)
            .ToList();
    }

    private HashSet<string> ExtractKeywords(string text)
    {
        // Очищаем текст от пунктуации и приводим к нижнему регистру
        text = Regex.Replace(text, @"[^\w\s]", " ").ToLower();
        
        // Разбиваем на слова
        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 2);
        return new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }

    private static double CalculateRelevance(HashSet<string> queryKeywords, HashSet<string> docKeywords)
    {
        if (!queryKeywords.Any() || !docKeywords.Any())
            return 0;

        var intersection = queryKeywords.Intersect(docKeywords, StringComparer.OrdinalIgnoreCase).Count();
        var union = queryKeywords.Union(docKeywords, StringComparer.OrdinalIgnoreCase).Count();

        return (double)intersection / union;
    }
}
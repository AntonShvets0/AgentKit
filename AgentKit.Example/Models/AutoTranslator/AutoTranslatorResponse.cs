namespace AgentKit.Example.Models.AutoTranslator;

public class AutoTranslatorResponse
{
    public string SourceText { get; set; }
    public Dictionary<string, string> TranslatedText { get; set; }
}
using System.Reflection;
using System.Resources;
using StopWords.Models;

namespace StopWords;

public static class StopWord
{
    public static string RemoveStopWords(
        string content, Language language
        )
    {
        var resourceManager = new ResourceManager("StopWords.StopWords", Assembly.GetExecutingAssembly());
        var wordString = resourceManager.GetString($"StopWord.data.{language.ToString().ToLower()}");
        if (wordString == null) return content;
                
        var words = wordString.Split("\n");
        foreach (var word in words)
            content = content.Replace(word, "", StringComparison.OrdinalIgnoreCase);

        return content;
    }
}
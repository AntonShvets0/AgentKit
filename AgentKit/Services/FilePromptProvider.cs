using System.Collections.Concurrent;
using System.Text.Json;
using AgentKit.Interfaces;
using AgentKit.Models;

namespace AgentKit.Services;

public class FilePromptProvider : IPromptProvider, IDisposable
{
    private readonly string _promptsDirectory;
    private readonly ConcurrentDictionary<string, PromptTemplate> _cache;
    private readonly SemaphoreSlim _cacheLock;
    private bool _isDisposed;

    public FilePromptProvider(string promptsDirectory)
    {
        if (string.IsNullOrEmpty(promptsDirectory))
            throw new ArgumentException("Prompts directory cannot be null or empty", nameof(promptsDirectory));
            
        if (!Directory.Exists(promptsDirectory))
            throw new DirectoryNotFoundException($"Directory not found: {promptsDirectory}");
            
        _promptsDirectory = promptsDirectory;
        _cache = new ConcurrentDictionary<string, PromptTemplate>();
        _cacheLock = new SemaphoreSlim(1, 1);
    }
    
    public async ValueTask<PromptTemplate> GetPromptAsync(string promptId)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FilePromptProvider));
            
        if (string.IsNullOrEmpty(promptId))
            throw new ArgumentException("Prompt ID cannot be null or empty", nameof(promptId));

        // Try get from cache first
        if (_cache.TryGetValue(promptId, out var cachedTemplate))
            return cachedTemplate;

        try
        {
            await _cacheLock.WaitAsync();
            
            // Double check after acquiring lock
            if (_cache.TryGetValue(promptId, out cachedTemplate))
                return cachedTemplate;
                
            var path = Path.Combine(_promptsDirectory, $"{promptId}.txt");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Prompt file not found: {promptId}.txt");
                
            var content = await File.ReadAllTextAsync(path);
            var template = new PromptTemplate()
            {
                Content = content
            };

            _cache.TryAdd(promptId, template);
            return template;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _cacheLock.Dispose();
        _cache.Clear();
        _isDisposed = true;
        
        GC.SuppressFinalize(this);
    }
}
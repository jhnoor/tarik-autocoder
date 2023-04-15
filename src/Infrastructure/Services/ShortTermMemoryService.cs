using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class ShortTermMemoryService : IShortTermMemoryService
{
    private readonly Dictionary<string, (string fileHash, string text)> _memory = new Dictionary<string, (string fileHash, string text)>();

    public void Memorize(string key, string fileHash, string text)
    {
        _memory[key] = (fileHash, text);
    }

    public string? Recall(string key, string fileHash)
    {
        if (_memory.TryGetValue(key, out var value))
        {
            if (value.fileHash == fileHash)
            {
                return value.text;
            }
        }

        return null;
    }
}

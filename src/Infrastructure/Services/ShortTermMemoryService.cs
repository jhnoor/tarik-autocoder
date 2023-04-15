using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class ShortTermMemoryService : IShortTermMemoryService
{
    private readonly Dictionary<string, (string fileHash, string text)> _memory = new Dictionary<string, (string fileHash, string text)>();

    public string Dump()
    {
        return $"""
        {_memory.Count} files in short-term memory:
        {string.Join(Environment.NewLine, _memory.Select(x => $"{x.Key} => {x.Value.text}"))}
        """;
    }

    public void Memorize(PathTo path, string fileHash, string text)
    {
        _memory[path.RelativePath] = (fileHash, text); // TODO maybe use absolute path?
    }

    public string? Recall(PathTo path, string fileHash)
    {
        if (_memory.TryGetValue(path.RelativePath, out var value))
        {
            if (value.fileHash == fileHash)
            {
                return value.text;
            }
        }

        return null;
    }
}

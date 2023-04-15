
namespace Tarik.Application.Common;

public interface IShortTermMemoryService
{
    void Memorize(string key, string fileHash, string text);
    string? Recall(string key, string fileHash);
}
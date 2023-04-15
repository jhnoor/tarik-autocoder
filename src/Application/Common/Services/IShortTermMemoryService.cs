
namespace Tarik.Application.Common;

public interface IShortTermMemoryService
{
    void Memorize(PathTo path, string fileHash, string text);
    string Dump();
    string? Recall(PathTo path, string fileHash);
}
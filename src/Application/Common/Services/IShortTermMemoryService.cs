
namespace Tarik.Application.Common;

public interface IShortTermMemoryService
{
    Task Memorize(PathTo path, CancellationToken cancellationToken);
    string Dump();
    string? Recall(PathTo path, string fileHash);
}
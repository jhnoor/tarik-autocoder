
namespace Tarik.Application.Common;

public interface IShortTermMemoryService
{
    Task Memorize(string repoOwner, string repoName, PathTo path, CancellationToken cancellationToken);
    string Dump(string repoOwner, string repoName);
    string? Recall(string repoOwner, string repoName, PathTo path, string fileHash);
}
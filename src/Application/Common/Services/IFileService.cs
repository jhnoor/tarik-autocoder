using Octokit;

namespace Tarik.Application.Common;

public interface IFileService
{
    Task<Reference> CreateBranch(string branchName, CancellationToken cancellationToken);
    Task CreateFile(string path, string content, Reference branch, CancellationToken cancellationToken);
    Task DeleteFile(string path, Reference branch, CancellationToken cancellationToken);
    Task<string> GetFileContent(string path, Reference branch, CancellationToken cancellationToken);
    Task EditFile(string path, string content, Reference branch, CancellationToken cancellationToken);
    Task<string> Tree(string path, CancellationToken cancellationToken);
}
namespace Tarik.Application.Common;

public interface IFileService
{
    Task CreateBranch(string branchName, CancellationToken cancellationToken);
    Task CreateFile(string path, string content, string branchName, CancellationToken cancellationToken);
    Task DeleteFile(string path, string branchName, CancellationToken cancellationToken);
    Task<string> GetFileContent(string path, string branchName, CancellationToken cancellationToken);
    Task EditFile(string path, string content, string branchName, CancellationToken cancellationToken);
    Task<string> Tree(string path, CancellationToken cancellationToken);
}
using Octokit;

namespace Tarik.Application.Common;

public interface IFileService : IDisposable
{
    Task<Reference> CreateBranch(string branchName, CancellationToken cancellationToken);
    Task CreateFile(CreateFilePlanStep createFileStep, Reference branch, CancellationToken cancellationToken);
    Task DeleteFile(DeleteFilePlanStep deleteFileStep, Reference branch, CancellationToken cancellationToken);
    Task<string> GetFileContent(string path, Reference branch, CancellationToken cancellationToken);
    Task EditFile(EditFilePlanStep editFileStep, Reference branch, CancellationToken cancellationToken);
    string GetPaths();
}
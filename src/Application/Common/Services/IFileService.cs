namespace Tarik.Application.Common;

public interface IFileService : IDisposable
{
    Task CreateFile(CreateFilePlanStep createFileStep, CancellationToken cancellationToken);
    Task DeleteFile(DeleteFilePlanStep deleteFileStep, CancellationToken cancellationToken);
    Task<string> GetFileContent(string path, CancellationToken cancellationToken);
    Task EditFile(EditFilePlanStep editFileStep, CancellationToken cancellationToken);
    Task<string> BranchName(CancellationToken cancellationToken);
    List<string> GetPaths(bool relativePath = true);
    string GetPathsAsString();
    Task Push(CancellationToken cancellationToken);
}
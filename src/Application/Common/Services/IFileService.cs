namespace Tarik.Application.Common;

public interface IFileService : IDisposable
{
    Task CreateFile(CreateFilePlanStep createFileStep, CancellationToken cancellationToken);
    Task DeleteFile(DeleteFilePlanStep deleteFileStep, CancellationToken cancellationToken);
    Task<string> GetFileContent(PathTo path, CancellationToken cancellationToken);
    Task EditFile(EditFilePlanStep editFileStep, CancellationToken cancellationToken);
    Task<string> BranchName(CancellationToken cancellationToken);
    List<PathTo> GetPaths();
    string GetPathsAsString();
    Task Push(CancellationToken cancellationToken);
    Task<string> DumpFiles(List<PathTo> filePaths, CancellationToken cancellationToken);
    string LocalDirectory();
}
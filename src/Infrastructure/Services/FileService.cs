using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileService : IFileService
{
    private readonly string _localDirectory;
    private readonly IGitHubClient _gitHubClient;
    private readonly string _gitHubPAT;
    private readonly WorkItem _workItem;
    private readonly IShellCommandService _shellCommandService;
    private readonly IShortTermMemoryService _shortTermMemoryService;
    private readonly ILogger<IFileService> _logger;
    private readonly List<PathTo> _paths = new();

    public FileService(
        string gitHubPAT,
        WorkItem workItem,
        IGitHubClientFactory gitHubClientFactory,
        IShellCommandService shellCommandService,
        IShortTermMemoryService shortTermMemoryService,
        ILogger<IFileService> logger)
    {
        _logger = logger;
        _gitHubPAT = gitHubPAT;
        _workItem = workItem;
        _shellCommandService = shellCommandService;
        _shortTermMemoryService = shortTermMemoryService;
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();
        _localDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_localDirectory);
        CloneRepository(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task CloneRepository(CancellationToken cancellationToken)
    {
        var currentUser = await _gitHubClient.User.Current();
        string repositoryWithPATUrl = $"https://tarik-tasktopr:{_gitHubPAT}@github.com/{_workItem.RepositoryOwner}/{_workItem.RepositoryName}.git";
        string branchName = $"tarik/{_workItem.Id}-{_workItem.Title.ToLower().Replace(' ', '-').Replace(',', '-').Replace('.', '-')}";

        await _shellCommandService.GitClone(repositoryWithPATUrl, _localDirectory, cancellationToken);
        await _shellCommandService.GitConfig(currentUser.Name, currentUser.Email, _localDirectory, cancellationToken);
        await _shellCommandService.GitCheckout(branchName, _localDirectory, cancellationToken);
    }

    public async Task CreateFile(CreateFilePlanStep createFileStep, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(createFileStep.PathTo.AbsolutePath, createFileStep.AISuggestedContent, cancellationToken);
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Create {createFileStep.PathTo.RelativePath}", _localDirectory, cancellationToken);
    }

    public async Task EditFile(EditFilePlanStep editFileStep, CancellationToken cancellationToken)
    {
        using var md5 = MD5.Create();
        var oldContent = await File.ReadAllTextAsync(editFileStep.PathTo.AbsolutePath, cancellationToken);
        var oldContentHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(oldContent)));
        var newContentHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(editFileStep.AISuggestedContent ?? "<null>")));

        if (oldContentHash == newContentHash)
        {
            _logger.LogInformation($"File {editFileStep.PathTo.RelativePath} has not changed, skipping commit");
            return;
        }

        await File.WriteAllTextAsync(editFileStep.PathTo.AbsolutePath, editFileStep.AISuggestedContent, cancellationToken);
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Edit {editFileStep.PathTo.RelativePath}", _localDirectory, cancellationToken);
    }

    public async Task DeleteFile(DeleteFilePlanStep deleteFileStep, CancellationToken cancellationToken)
    {
        File.Delete(deleteFileStep.PathTo.AbsolutePath);
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Delete {deleteFileStep.PathTo.RelativePath}", _localDirectory, cancellationToken);
    }

    public async Task<string> GetFileContent(PathTo path, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(path.AbsolutePath, cancellationToken);
    }

    public string GetPathsAsString()
    {
        return GetPaths()
            .Select(x => x.RelativePath)
            .Aggregate((x, y) => $"{x}{Environment.NewLine}{y}");
    }

    public List<PathTo> GetPaths()
    {
        if (_paths.Count == 0)
        {
            _paths.AddRange(FileHelper.GetTree(_localDirectory).Flatten(_localDirectory));
        }

        return _paths;
    }


    public async Task<string> BranchName(CancellationToken cancellationToken)
    {
        return await _shellCommandService.GitBranchName(_localDirectory, cancellationToken);
    }

    public async Task<string> DumpFiles(List<PathTo> filePaths, CancellationToken cancellationToken)
    {
        if (filePaths.Count == 0)
        {
            return "\n";
        }

        var relevantFiles = await Task.WhenAll(filePaths.Select(file => GetFileContent(file, cancellationToken)));
        var relevantFilesDump = relevantFiles.Select((content, index) => $"""
            - {filePaths[index].RelativePath}

            ```
            {content}
            ```
        """);

        return $"""
    
            For added context, here are the relevant files:

            <relevantFiles>
            {string.Join("", relevantFilesDump)}
            </relevantFiles>
        """;
    }

    public void Dispose()
    {
        if (_localDirectory != null)
        {
            try
            {
                Directory.Delete(_localDirectory, true);
                _logger.LogDebug($"Deleted temporary repository folder: {_localDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting temporary repository folder {_localDirectory}");
            }
        }
    }

    public async Task Push(CancellationToken cancellationToken)
    {
        var branchName = await BranchName(cancellationToken);
        await _shellCommandService.GitPush(branchName, _localDirectory, cancellationToken);
    }

    public string LocalDirectory()
    {
        return _localDirectory;
    }
}

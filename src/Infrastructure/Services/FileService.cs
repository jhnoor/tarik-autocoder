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
    private readonly ILogger<IFileService> _logger;

    public FileService(string gitHubPAT, WorkItem workItem, IGitHubClientFactory gitHubClientFactory, IShellCommandService shellCommandService, ILogger<IFileService> logger)
    {
        _logger = logger;
        _gitHubPAT = gitHubPAT;
        _workItem = workItem;
        _shellCommandService = shellCommandService;
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();
        _localDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_localDirectory);
        CloneRepository(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task CloneRepository(CancellationToken cancellationToken)
    {
        var currentUser = await _gitHubClient.User.Current();
        string repositoryWithPATUrl = $"https://tarik-tasktopr:{_gitHubPAT}@github.com/{_workItem.RepositoryOwner}/{_workItem.RepositoryName}.git";
        string branchName = $"tarik/{_workItem.Id}-{_workItem.Title.ToLower().Replace(' ', '-')}";

        await _shellCommandService.GitClone(repositoryWithPATUrl, _localDirectory, cancellationToken);
        await _shellCommandService.GitConfig(currentUser.Name, currentUser.Email, _localDirectory, cancellationToken);
        await _shellCommandService.GitCheckout(branchName, _localDirectory, cancellationToken);
    }

    public async Task CreateFile(CreateFilePlanStep createFileStep, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(Path.Combine(_localDirectory, createFileStep.Path), createFileStep.AISuggestedContent, cancellationToken);
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Create {createFileStep.Path}", _localDirectory, cancellationToken);
    }

    public async Task DeleteFile(DeleteFilePlanStep deleteFileStep, CancellationToken cancellationToken)
    {
        File.Delete(Path.Combine(_localDirectory, deleteFileStep.Path));
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Delete {deleteFileStep.Path}", _localDirectory, cancellationToken);
    }

    public async Task<string> GetFileContent(string path, CancellationToken cancellationToken)
    {
        return await File.ReadAllTextAsync(Path.Combine(_localDirectory, path), cancellationToken);
    }

    public string GetPaths()
    {
        return FileHelper.GetTree(_localDirectory).SerializePaths(_localDirectory);
    }

    public async Task EditFile(EditFilePlanStep editFileStep, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(Path.Combine(_localDirectory, editFileStep.Path), editFileStep.AISuggestedContent, cancellationToken);
        await _shellCommandService.GitAddAll(_localDirectory, cancellationToken);
        await _shellCommandService.GitCommit($"Edit {editFileStep.Path}", _localDirectory, cancellationToken);
    }

    public async Task<string> BranchName(CancellationToken cancellationToken)
    {
        return await _shellCommandService.GitBranchName(_localDirectory, cancellationToken);
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
}

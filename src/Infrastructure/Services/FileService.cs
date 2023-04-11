using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileService : IFileService
{
    private string _localDirectory;
    private bool _isRepositoryCloned;
    private readonly IGitHubClient _gitHubClient;
    private readonly string _gitHubPAT;
    private readonly WorkItem _workItem;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<IFileService> _logger;

    public FileService(string gitHubPAT, WorkItem workItem, IGitHubClientFactory gitHubClientFactory, ILogger<IFileService> logger)
    {
        _logger = logger;
        _gitHubPAT = gitHubPAT;
        _workItem = workItem;
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();
        _localDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_localDirectory);
    }

    private async Task CloneRepository(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        if (_isRepositoryCloned)
        {
            _semaphore.Release();
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone https://tarik-tasktopr:{_gitHubPAT}@github.com/{_workItem.RepositoryOwner}/{_workItem.RepositoryName}.git {_localDirectory}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogError(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                _logger.LogDebug($"Repository cloned successfully to {_localDirectory}");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to clone repository to {_localDirectory}, exit code: {process.ExitCode}. Error: {error}");
            }

            _isRepositoryCloned = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CreateFile(CreateFilePlanStep createFileStep, Reference branch, CancellationToken cancellationToken)
    {
        var createFileRequest = new CreateFileRequest($"Create {createFileStep.Path}", createFileStep.AISuggestedContent, branch.Ref, true);
        try
        {
            await _gitHubClient.Repository.Content.CreateFile(_workItem.RepositoryOwner, _workItem.RepositoryName, createFileStep.Path, createFileRequest);
        }
        catch (Octokit.ApiValidationException e)
        {
            if (e.Message.Contains("\"sha\" wasn't supplied"))
            {
                // File already exists
                return;
            }
            else
            {
                throw;
            }
        }
    }

    public Task DeleteFile(DeleteFilePlanStep deleteFileStep, Reference branch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetFileContent(string path, Reference branch, CancellationToken cancellationToken)
    {
        var bytes = await _gitHubClient.Repository.Content.GetRawContentByRef(_workItem.RepositoryOwner, _workItem.RepositoryName, path, branch.Ref);
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task<string> GetPaths(CancellationToken cancellationToken)
    {
        await CloneRepository(cancellationToken);
        return FileHelper.GetTree(_localDirectory).SerializePaths(_localDirectory);
    }

    public async Task EditFile(EditFilePlanStep editFileStep, Reference branch, CancellationToken cancellationToken)
    {
        var existingFileHash = (await _gitHubClient.Repository.Content.GetAllContentsByRef(_workItem.RepositoryOwner, _workItem.RepositoryName, editFileStep.Path, branch.Ref)).Single().Sha;
        var updateFileRequest = new UpdateFileRequest($"Update {editFileStep.Path}", editFileStep.AISuggestedContent, existingFileHash, branch.Ref, true);
        await _gitHubClient.Repository.Content.UpdateFile(_workItem.RepositoryOwner, _workItem.RepositoryName, editFileStep.Path, updateFileRequest);
    }

    public async Task<Reference> CreateBranch(string branchName, CancellationToken cancellationToken)
    {
        try
        {
            var main = await _gitHubClient.Git.Reference.Get(_workItem.RepositoryOwner, _workItem.RepositoryName, "heads/main");
            return await _gitHubClient.Git.Reference.Create(_workItem.RepositoryOwner, _workItem.RepositoryName, new NewReference($"refs/heads/{branchName}", main.Object.Sha));
        }
        catch (Octokit.ApiValidationException e)
        {
            if (e.Message.Contains("Reference already exists"))
            {
                return await _gitHubClient.Git.Reference.Get(_workItem.RepositoryOwner, _workItem.RepositoryName, $"refs/heads/{branchName}");
            }
            else
            {
                throw;
            }
        }
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

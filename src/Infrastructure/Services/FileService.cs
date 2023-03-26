using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileService : IFileService
{
    private string _localDirectory;
    private readonly IGitHubClient _gitHubClient;
    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly ILogger<IFileService> _logger;

    public FileService(IOptions<AppSettings> appSettings, IGitHubClientFactory gitHubClientFactory, ILogger<IFileService> logger)
    {
        _logger = logger;
        var gitHubSettings = appSettings.Value.GitHub;

        if (gitHubSettings == null)
        {
            throw new ArgumentNullException(nameof(gitHubSettings));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.PAT))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.PAT));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.Owner))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.Owner));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.Repo))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.Repo));
        }

        _repoOwner = gitHubSettings.Owner;
        _repoName = gitHubSettings.Repo;
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();

        _localDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_localDirectory);

        CloneRepository();
    }

    private void CloneRepository()
    {
        var repoUrl = $"https://github.com/{_repoOwner}/{_repoName}.git";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone {repoUrl} {_localDirectory}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (sender, e) => _logger.LogDebug(e.Data);
        process.ErrorDataReceived += (sender, e) => _logger.LogError(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            _logger.LogDebug($"Repository cloned successfully to {_localDirectory}");
        }
        else
        {
            throw new InvalidOperationException($"Failed to clone repository to {_localDirectory} from {repoUrl}");
        }
    }

    public async Task CreateFile(string path, string content, Reference branch, CancellationToken cancellationToken)
    {
        var createFileRequest = new CreateFileRequest($"Create {path}", content, branch.Ref, true);
        try
        {
            await _gitHubClient.Repository.Content.CreateFile(_repoOwner, _repoName, path, createFileRequest);
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

    public Task DeleteFile(string path, Reference branch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetFileContent(string path, Reference branch, CancellationToken cancellationToken)
    {
        var bytes = await _gitHubClient.Repository.Content.GetRawContentByRef(_repoOwner, _repoName, path, branch.Ref);
        return Encoding.UTF8.GetString(bytes);
    }

    public string GetPaths()
    {
        return FileHelper.GetTree(_localDirectory).SerializePaths(_localDirectory);
    }

    public async Task EditFile(string path, string content, Reference branch, CancellationToken cancellationToken)
    {
        var existingFileHash = (await _gitHubClient.Repository.Content.GetAllContentsByRef(_repoOwner, _repoName, path, branch.Ref)).Single().Sha;
        var updateFileRequest = new UpdateFileRequest($"Update {path}", content, existingFileHash, branch.Ref, true);
        await _gitHubClient.Repository.Content.UpdateFile(_repoOwner, _repoName, path, updateFileRequest);
    }

    public async Task<Reference> CreateBranch(string branchName, CancellationToken cancellationToken)
    {
        try
        {
            var main = await _gitHubClient.Git.Reference.Get(_repoOwner, _repoName, "heads/main");
            return await _gitHubClient.Git.Reference.Create(_repoOwner, _repoName, new NewReference($"refs/heads/{branchName}", main.Object.Sha));
        }
        catch (Octokit.ApiValidationException e)
        {
            if (e.Message.Contains("Reference already exists"))
            {
                return await _gitHubClient.Git.Reference.Get(_repoOwner, _repoName, $"refs/heads/{branchName}");
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

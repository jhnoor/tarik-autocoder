using System.Text;
using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileService : IFileService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly string _repoOwner;
    private readonly string _repoName;

    public FileService(IOptions<AppSettings> appSettings, IGitHubClientFactory gitHubClientFactory)
    {
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

    public async Task<string> Tree(string path = "/", CancellationToken cancellationToken = default)
    {
        var files = await _gitHubClient.Repository.Content.GetAllContents(_repoOwner, _repoName, path);
        return string.Join(Environment.NewLine, files.Select(f => f.Name));
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
}

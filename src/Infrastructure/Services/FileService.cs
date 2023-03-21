using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileService : IFileService
{
    private readonly GitHubClient _gitHubClient;
    private readonly string _repoOwner;
    private readonly string _repoName;

    public FileService(IOptions<AppSettings> appSettings)
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

        _gitHubClient = new GitHubClient(new ProductHeaderValue("Tarik"))
        {
            Credentials = new Credentials(gitHubSettings.PAT)
        };
    }

    public async Task CreateFile(string path, string content, string branchName, CancellationToken cancellationToken)
    {
        await _gitHubClient.Repository.Content.CreateFile(_repoOwner, _repoName, path, new CreateFileRequest("Create file", content, $"refs/heads/{branchName}"));
    }

    public Task DeleteFile(string path, string branchName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetFileContent(string path, string branchName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> Tree(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task EditFile(string path, string content, string branchName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task CreateBranch(string branchName, CancellationToken cancellationToken)
    {
        try
        {
            var main = await _gitHubClient.Git.Reference.Get(_repoOwner, _repoName, "heads/main");
            var newBranch = await _gitHubClient.Git.Reference.Create(_repoOwner, _repoName, new NewReference($"refs/heads/{branchName}", main.Object.Sha));
        }
        catch (Octokit.ApiValidationException e)
        {
            if (e.Message.Contains("Reference already exists"))
            {
                return;
            }
            else
            {
                throw;
            }
        }
    }
}

using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class GitHubClientFactory : IGitHubClientFactory
{
    private readonly GitHubSettings _gitHubSettings;

    public GitHubClientFactory(IOptions<AppSettings> appSettings)
    {
        if (appSettings.Value.GitHub == null)
        {
            throw new ArgumentNullException(nameof(_gitHubSettings));
        }

        _gitHubSettings = appSettings.Value.GitHub;

        if (string.IsNullOrWhiteSpace(_gitHubSettings.PAT))
        {
            throw new ArgumentNullException(nameof(_gitHubSettings.PAT));
        }
    }

    public IGitHubClient CreateGitHubClient()
    {
        return new GitHubClient(new ProductHeaderValue("Tarik"))
        {
            Credentials = new Credentials(_gitHubSettings.PAT)
        };
    }
}
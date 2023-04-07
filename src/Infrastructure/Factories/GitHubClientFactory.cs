using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class GitHubClientFactory : IGitHubClientFactory
{
    private readonly string _gitHubPAT;

    public GitHubClientFactory(IOptions<AppSettings> appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.Value.GitHubPAT))
        {
            throw new ArgumentNullException(nameof(AppSettings.GitHubPAT));
        }

        _gitHubPAT = appSettings.Value.GitHubPAT;
    }

    public IGitHubClient CreateGitHubClient()
    {
        return new GitHubClient(new ProductHeaderValue("Tarik"))
        {
            Credentials = new Credentials(_gitHubPAT)
        };
    }
}
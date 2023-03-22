using Octokit;

namespace Tarik.Application.Common;

public interface IGitHubClientFactory
{
    IGitHubClient CreateGitHubClient();
}
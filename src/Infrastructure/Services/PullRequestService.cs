using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly string _repoOwner;
    private readonly string _repoName;
    public PullRequestService(IOptions<AppSettings> appSettings, IGitHubClientFactory gitHubClientFactory)
    {
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();

        _repoName = appSettings.Value.GitHub!.Repo!; // TODO code smell
        _repoOwner = appSettings.Value.GitHub.Owner!; // TODO code smell
    }

    public async Task<Application.Common.PullRequest> CreatePullRequest(WorkItem workItem, string sourceBranchRef, CancellationToken cancellationToken)
    {
        var targetBranch = await _gitHubClient.Git.Reference.Get(_repoOwner, _repoName, "heads/main");
        var existingPr = await _gitHubClient.PullRequest.GetAllForRepository(_repoOwner, _repoName, new PullRequestRequest
        {
            State = ItemStateFilter.Open,
            Head = sourceBranchRef,
            Base = targetBranch.Ref,
        });

        if (existingPr.Any())
        {
            return new Application.Common.PullRequest(existingPr.First());
        }

        var prTitle = $"Implements: {workItem.Title}";
        var createPullRequest = new NewPullRequest(prTitle, sourceBranchRef, targetBranch.Ref)
        {
            Body = $"Implements: #{workItem.Id}",
        };
        var pr = await _gitHubClient.PullRequest.Create(_repoOwner, _repoName, createPullRequest);
        return new Application.Common.PullRequest(pr);
    }
}
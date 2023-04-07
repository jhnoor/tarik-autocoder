using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _gitHubClient;
    public PullRequestService(IGitHubClientFactory gitHubClientFactory)
    {
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();
    }

    public async Task<Application.Common.PullRequest> CreatePullRequest(WorkItem workItem, string sourceBranchRef, CancellationToken cancellationToken)
    {
        var targetBranch = await _gitHubClient.Git.Reference.Get(workItem.RepositoryOwner, workItem.RepositoryName, "heads/main");
        var pullRequests = await _gitHubClient.PullRequest.GetAllForRepository(workItem.RepositoryOwner, workItem.RepositoryName, new PullRequestRequest
        {
            State = ItemStateFilter.Open,
            Head = sourceBranchRef,
            Base = targetBranch.Ref,
        });

        var existingPr = pullRequests.SingleOrDefault(x => x.Head.Ref == sourceBranchRef);

        if (existingPr != null)
        {
            return new Application.Common.PullRequest(existingPr);
        }

        var prTitle = $"Implements: {workItem.Title}";
        var createPullRequest = new NewPullRequest(prTitle, sourceBranchRef, targetBranch.Ref)
        {
            Body = $"Implements: #{workItem.Id}",
        };
        var pr = await _gitHubClient.PullRequest.Create(workItem.RepositoryOwner, workItem.RepositoryName, createPullRequest);
        return new Application.Common.PullRequest(pr);
    }
}
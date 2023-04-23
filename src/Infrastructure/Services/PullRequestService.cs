using Microsoft.Extensions.Logging;
using Octokit;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IWorkItemService _workItemService;
    private readonly ILogger<IPullRequestService> _logger;

    public PullRequestService(
        IGitHubClientFactory gitHubClientFactory,
        IWorkItemService workItemService,
        ILogger<IPullRequestService> logger)
    {
        _gitHubClient = gitHubClientFactory.CreateGitHubClient();
        _workItemService = workItemService;
        _logger = logger;
    }

    public async Task CreatePullRequest(WorkItem workItem, string sourceBranchRef, CancellationToken cancellationToken)
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
            return;
        }

        var prTitle = $"Implements: {workItem.Title}";
        var createPullRequest = new NewPullRequest(prTitle, sourceBranchRef, targetBranch.Ref)
        {
            Body = $"Implements: #{workItem.Id}",
        };
        await _gitHubClient.PullRequest.Create(workItem.RepositoryOwner, workItem.RepositoryName, createPullRequest);
        return;
    }

    public async Task<List<ReviewPullRequest>> GetPrsAssignedToTarikForReview(CancellationToken cancellationToken)
    {
        var issues = await _workItemService.GetIssuesAssignedToTarik(cancellationToken);
        var repos = issues.Select(x => new { x.RepositoryOwner, x.RepositoryName }).Distinct();

        var list = new List<ReviewPullRequest>();
        foreach (var repo in repos)
        {
            var prs = await _gitHubClient.PullRequest.GetAllForRepository(repo.RepositoryOwner, repo.RepositoryName, new PullRequestRequest
            {
                State = ItemStateFilter.Open,
            });

            foreach (var pr in prs)
            {
                IReadOnlyList<PullRequestReviewComment>? comments = null;

                comments = await _gitHubClient.PullRequest.ReviewComment.GetAll(repo.RepositoryOwner, repo.RepositoryName, pr.Number);

                var reviewPr = new ReviewPullRequest(pr, comments, repo.RepositoryOwner, repo.RepositoryName);
                list.Add(reviewPr);
            }
        }

        return list;
    }
}
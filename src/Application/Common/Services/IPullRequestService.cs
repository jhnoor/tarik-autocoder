

namespace Tarik.Application.Common;

public interface IPullRequestService
{
    Task<PullRequest> CreatePullRequest(WorkItem workItem, string sourceBranchRef, CancellationToken cancellationToken);
}
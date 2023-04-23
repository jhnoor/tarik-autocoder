namespace Tarik.Application.Common;

public interface IPullRequestService
{
    Task CreatePullRequest(WorkItem workItem, string sourceBranchRef, CancellationToken cancellationToken);
    Task<List<ReviewPullRequest>> GetPrsAssignedToTarikForReview(CancellationToken cancellationToken);
}
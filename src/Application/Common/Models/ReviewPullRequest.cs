namespace Tarik.Application.Common;

public class ReviewPullRequest
{
    public long Id { get; }
    public string Title { get; }
    public string RepositoryOwner { get; }
    public string RepositoryName { get; }
    public List<ReviewComment> Comments { get; }
    public string BranchName { get; set; }

    public ReviewPullRequest(Octokit.PullRequest pr, IReadOnlyList<Octokit.PullRequestReviewComment>? comments, string repoOwner, string repoName)
    {
        Id = pr.Id;
        Title = pr.Title;
        RepositoryOwner = repoOwner;
        RepositoryName = repoName;
        BranchName = pr.Head.Ref;
        Comments = comments?.Select(c => new ReviewComment(c)).ToList() ?? new List<ReviewComment>();
    }
}
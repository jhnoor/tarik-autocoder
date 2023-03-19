using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Tarik.Application.Common.DTOs;

public class PullRequestDTO
{
    public string Author { get; }
    public string Title { get; }
    public int Id { get; }
    public string Url { get; }
    public string SourceBranch { get; }
    public string TargetBranch { get; }
    public List<string> WorkItemIds { get; }
    public DateTime ClosedDate { get; }

    public PullRequestDTO(GitPullRequest pr)
    {
        Author = pr.CreatedBy.DisplayName;
        Title = pr.Title;
        Id = pr.PullRequestId;
        Url = pr.Url;
        SourceBranch = pr.SourceRefName;
        TargetBranch = pr.TargetRefName;
        WorkItemIds = pr.WorkItemRefs != null ? pr.WorkItemRefs.Select(w => w.Id).ToList() : new List<string>();
        ClosedDate = pr.ClosedDate;
    }
}
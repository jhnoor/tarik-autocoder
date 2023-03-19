namespace Tarik.Application.Common.DTOs;

public class UserActivitySummaryDTO
{
    public string UserEmail { get; }
    public DateTime? FromDate { get; }
    public DateTime? ToDate { get; }

    public int TotalCommits { get; }
    public int TotalWorkItems { get; }
    public int TotalPullRequests { get; }

    public List<GitCommitDTO> GitCommits { get; }
    public List<WorkItemUpdateDTO> WorkItemsUpdates { get; }
    public List<PullRequestDTO> PullRequests { get; }

    public UserActivitySummaryDTO(
        string userEmail,
        DateTime? fromDate,
        DateTime? toDate,
        int totalCommits,
        int totalWorkItems,
        int totalPullRequests,
        List<GitCommitDTO>? gitCommits = null,
        List<WorkItemUpdateDTO>? workItemsUpdates = null,
        List<PullRequestDTO>? pullRequests = null)
    {
        UserEmail = userEmail;
        FromDate = fromDate;
        ToDate = toDate;
        TotalCommits = totalCommits;
        TotalWorkItems = totalWorkItems;
        TotalPullRequests = totalPullRequests;
        GitCommits = gitCommits ?? new List<GitCommitDTO>();
        WorkItemsUpdates = workItemsUpdates ?? new List<WorkItemUpdateDTO>();
        PullRequests = pullRequests ?? new List<PullRequestDTO>();
    }
}

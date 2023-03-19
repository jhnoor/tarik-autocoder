namespace Tarik.Application.Common.DTOs;

public class SmartUserActivitySummaryDTO : UserActivitySummaryDTO
{
    public string SmartSummary { get; }

    public SmartUserActivitySummaryDTO(string smartSummary, UserActivitySummaryDTO userActivitySummaryDTO)
        : base(
            userActivitySummaryDTO.UserEmail,
            userActivitySummaryDTO.FromDate,
            userActivitySummaryDTO.ToDate,
            userActivitySummaryDTO.TotalCommits,
            userActivitySummaryDTO.TotalWorkItems,
            userActivitySummaryDTO.TotalPullRequests,
            userActivitySummaryDTO.GitCommits,
            userActivitySummaryDTO.WorkItemsUpdates,
            userActivitySummaryDTO.PullRequests)
    {
        SmartSummary = smartSummary;
    }
}

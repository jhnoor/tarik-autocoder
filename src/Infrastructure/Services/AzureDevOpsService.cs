using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Profile;
using Microsoft.VisualStudio.Services.WebApi;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Infrastructure.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly IAzureDevOpsHttpClientFactory _azureDevOpsHttpClientFactory;


    public AzureDevOpsService(IAzureDevOpsHttpClientFactory httpClientFactory)
    {
        _azureDevOpsHttpClientFactory = httpClientFactory;

    }

    public async Task<List<GitCommitDTO>> GetAllGitCommitsForUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        List<GitCommitDTO> gitCommits = new();
        var client = _azureDevOpsHttpClientFactory.CreateGitHttpClient(personalAccessToken, azureDevOpsUrl);

        var gitQuery = new GitQueryCommitsCriteria()
        {
            Author = userEmail,
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            ToDate = toDate.ToString("yyyy-MM-dd")
        };
        var repositories = await client.GetRepositoriesAsync(projectName);

        foreach (var repository in repositories)
        {
            List<GitCommitRef> commits = await client.GetCommitsAsync(projectName, repository.Id, gitQuery, cancellationToken: cancellationToken);

            gitCommits.AddRange(commits
                .Where(c => c.Author.Date >= fromDate && c.Author.Date <= toDate)
                .DistinctBy(c => c.CommitId)
                .Select(c => new GitCommitDTO(repository.Name, c.CommitId, c.Author.Date, c.Comment))
                .ToList());
        }

        gitCommits.Sort((x, y) => DateTime.Compare(x.Date, y.Date));
        return gitCommits;
    }

    public async Task<List<WorkItemUpdateDTO>> GetAllWorkItemUpdatesForUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        List<WorkItemUpdateDTO> workItemUpdates = new();

        var client = _azureDevOpsHttpClientFactory.CreateWorkItemTrackingHttpClient(personalAccessToken, azureDevOpsUrl);

        var fromDateString = fromDate.ToString("yyyy-MM-dd");
        var toDateString = toDate.ToString("yyyy-MM-dd");
        if (fromDate.AddHours(24) > toDate)
        {
            fromDateString = fromDate.AddHours(-24).ToString("yyyy-MM-dd");
        }

        var wiql = new Wiql
        {
            Query = @$"
SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.ChangedDate], [System.ChangedBy]
FROM WorkItems
WHERE [System.ChangedBy] = '{userEmail}'
AND [System.ChangedDate] >= '{fromDateString}'
AND [System.ChangedDate] <= '{toDateString}'
ORDER BY [System.ChangedDate] DESC
            "
        };

        var result = await client.QueryByWiqlAsync(wiql, projectName, top: 100, cancellationToken: cancellationToken);
        var workItems = await client.GetWorkItemsAsync(result.WorkItems.Select(wi => wi.Id).ToArray(), expand: WorkItemExpand.Relations, cancellationToken: cancellationToken);

        foreach (var workItem in workItems)
        {
            if (workItem.Id == null)
                continue;

            DateTime.TryParse(workItem.Fields["System.ChangedDate"].ToString(), out DateTime changedDate);
            if (changedDate < fromDate || changedDate > toDate)
                continue;
            workItemUpdates.Add(
                new WorkItemUpdateDTO(
                    workItem.Id.Value,
                    workItem.Fields["System.Title"] as string,
                    workItem.Fields["System.WorkItemType"] as string,
                    workItem.Fields["System.State"] as string,
                    workItem.Fields["System.AssignedTo"] as IdentityRef,
                    changedDate == default ? null : changedDate
                )
            );
        }

        return workItemUpdates;
    }

    public async Task<List<PullRequestDTO>> GetAllPullRequestsCompletedByUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, Guid creatorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        List<PullRequestDTO> pullRequests = new();

        var client = _azureDevOpsHttpClientFactory.CreateGitHttpClient(personalAccessToken, azureDevOpsUrl);

        var repositories = await client.GetRepositoriesAsync(projectName);

        foreach (var repository in repositories)
        {
            var pullRequestQuery = new GitPullRequestSearchCriteria()
            {
                CreatorId = creatorId,
                Status = PullRequestStatus.Completed,
            };

            List<GitPullRequest> prs = await client.GetPullRequestsAsync(projectName, repository.Id, pullRequestQuery, cancellationToken: cancellationToken);

            pullRequests.AddRange(prs
                .Where(pr => pr.ClosedDate >= fromDate && pr.ClosedDate <= toDate)
                .DistinctBy(pr => pr.PullRequestId)
                .Select(pr => new PullRequestDTO(pr))
                .ToList());
        }

        return pullRequests;
    }

    public async Task<Profile> GetUser(string personalAccessToken, string azureDevOpsUrl, string userEmail, CancellationToken cancellationToken)
    {
        var client = _azureDevOpsHttpClientFactory.CreateProfileHttpClient(personalAccessToken, azureDevOpsUrl);

        return await client.GetProfileAsync(new ProfileQueryContext(AttributesScope.Core), cancellationToken: cancellationToken);
    }
}
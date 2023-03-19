using Microsoft.VisualStudio.Services.Profile;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.Common;

public interface IAzureDevOpsService
{
    Task<Profile> GetUser(string personalAccessToken, string azureDevOpsUrl, string userEmail, CancellationToken cancellationToken);
    Task<List<GitCommitDTO>> GetAllGitCommitsForUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
    Task<List<WorkItemUpdateDTO>> GetAllWorkItemUpdatesForUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
    Task<List<PullRequestDTO>> GetAllPullRequestsCompletedByUser(string personalAccessToken, string azureDevOpsUrl, string projectName, string userEmail, Guid creatorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
}
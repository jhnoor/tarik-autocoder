using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Infrastructure;

/// <summary>
/// The GitHub implementation of <see cref="IWorkItemApiClient"/>.
/// </summary>
public class GitHubIssuesApiClient : IWorkItemApiClient
{
    private readonly GitHubClient _gitHubClient;
    private readonly string _repoOwner;
    private readonly string _repoName;

    public GitHubIssuesApiClient(IOptions<AppSettings> appSettings)
    {
        var gitHubSettings = appSettings.Value.GitHub;

        if (gitHubSettings == null)
        {
            throw new ArgumentNullException(nameof(gitHubSettings));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.PAT))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.PAT));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.Owner))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.Owner));
        }

        if (string.IsNullOrWhiteSpace(gitHubSettings.Repo))
        {
            throw new ArgumentNullException(nameof(gitHubSettings.Repo));
        }

        _repoOwner = gitHubSettings.Owner;
        _repoName = gitHubSettings.Repo;

        _gitHubClient = new GitHubClient(new ProductHeaderValue("Tarik"))
        {
            Credentials = new Credentials(gitHubSettings.PAT)
        };
    }

    public async Task<int> Comment(int id, string comment, CancellationToken cancellationToken)
    {
        var response = await _gitHubClient.Issue.Comment.Create(_repoOwner, _repoName, id, comment);
        return response.Id;
    }

    public async Task<List<WorkItemDTO>> GetOpenWorkItems(CancellationToken cancellationToken)
    {
        var request = new RepositoryIssueRequest
        {
            Filter = IssueFilter.Assigned,
            State = ItemStateFilter.Open
        };

        var currentUser = await _gitHubClient.User.Current();
        var issues = await _gitHubClient.Issue.GetAllForRepository(_repoOwner, _repoName, request);

        return issues
            .Where(issue => issue.Assignees.Select(a => a.Id).Contains(currentUser.Id))
            .Select(issue => new WorkItemDTO(issue))
            .ToList();
    }

    public Task LabelAwaitingCodeReview(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task LabelAwaitingImplementation(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task LabelAwaitingPlanApproval(int id, CancellationToken cancellationToken)
    {
        await _gitHubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, id, new string[] { StateMachineLabel.AutoCodeAwaitingPlanApproval.ToLabelString() });
    }
}
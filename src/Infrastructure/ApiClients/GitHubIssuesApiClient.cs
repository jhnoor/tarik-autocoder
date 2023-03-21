using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;


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

    public async Task<int> Comment(int workItemId, string comment, CancellationToken cancellationToken)
    {
        var response = await _gitHubClient.Issue.Comment.Create(_repoOwner, _repoName, workItemId, comment);
        return response.Id;
    }

    public async Task<List<Comment>> GetCommentsAsync(int workItemId)
    {
        var currentUser = await _gitHubClient.User.Current();
        var comments = await _gitHubClient.Issue.Comment.GetAllForIssue(_repoOwner, _repoName, workItemId);
        return comments.Select(c => new Comment(c, currentUser)).ToList();
    }

    public async Task<List<WorkItem>> GetOpenWorkItems(CancellationToken cancellationToken)
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
            .Select(issue => new WorkItem(issue))
            .ToList();
    }

    public async Task Label(int workItemId, List<StateMachineLabel> addLabels, List<StateMachineLabel> removeLabels, CancellationToken cancellationToken)
    {
        foreach (var label in removeLabels)
        {
            try
            {
                await _gitHubClient.Issue.Labels.RemoveFromIssue(_repoOwner, _repoName, workItemId, label.ToLabelString());
            }
            catch (Octokit.NotFoundException)
            {
                // Ignore
            }
        }

        var labels = addLabels.Select(l => l.ToLabelString()).ToArray();
        await _gitHubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, workItemId, labels);
    }

    public async Task Label(int workItemId, StateMachineLabel replacementLabel, CancellationToken cancellationToken)
    {
        var allStateMachineLabels = Enum.GetValues(typeof(StateMachineLabel)).Cast<StateMachineLabel>().Where(l => l != StateMachineLabel.Init).ToList();
        await Label(workItemId, new List<StateMachineLabel> { replacementLabel }, allStateMachineLabels, cancellationToken);
    }

    public Task LabelAwaitingCodeReview(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task LabelAwaitingImplementation(int id, Comment approvedPlanComment, CancellationToken cancellationToken)
    {
        await _gitHubClient.Issue.Comment.Update(_repoOwner, _repoName, approvedPlanComment.Id, $"{approvedPlanComment.Body}\n\n Plan approved! ✅");

        await _gitHubClient.Issue.Labels.RemoveFromIssue(_repoOwner, _repoName, id, StateMachineLabel.AutoCodeAwaitingPlanApproval.ToLabelString());
        await _gitHubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, id, new string[] { StateMachineLabel.AutoCodeAwaitingImplementation.ToLabelString() });
    }

    public async Task LabelAwaitingPlanApproval(int id, CancellationToken cancellationToken)
    {
        await _gitHubClient.Issue.Labels.AddToIssue(_repoOwner, _repoName, id, new string[] { StateMachineLabel.AutoCodeAwaitingPlanApproval.ToLabelString() });
    }
}
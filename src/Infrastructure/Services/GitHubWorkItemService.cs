using Microsoft.Extensions.Options;
using Octokit;
using Tarik.Application.Common;


namespace Tarik.Infrastructure;

/// <summary>
/// The GitHub implementation of <see cref="IWorkItemService"/>.
/// </summary>
public class GitHubWorkItemService : IWorkItemService
{
    private readonly GitHubClient _gitHubClient;

    public GitHubWorkItemService(IOptions<AppSettings> appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.Value.GitHubPAT))
        {
            throw new ArgumentNullException(nameof(AppSettings.GitHubPAT));
        }

        _gitHubClient = new GitHubClient(new ProductHeaderValue("Tarik"))
        {
            Credentials = new Credentials(appSettings.Value.GitHubPAT)
        };
    }

    public async Task<int> Comment(WorkItem workItem, string comment, CancellationToken cancellationToken)
    {
        var response = await _gitHubClient.Issue.Comment.Create(workItem.RepositoryOwner, workItem.RepositoryName, workItem.Id, comment);
        return response.Id;
    }

    public async Task<List<Comment>> GetCommentsAsync(WorkItem workItem)
    {
        var currentUser = await _gitHubClient.User.Current();
        var comments = await _gitHubClient.Issue.Comment.GetAllForIssue(workItem.RepositoryOwner, workItem.RepositoryName, workItem.Id);
        return comments.Select(c => new Comment(c, currentUser)).ToList();
    }

    public async Task<List<WorkItem>> GetIssuesAssignedToTarik(CancellationToken cancellationToken)
    {
        var request = new RepositoryIssueRequest
        {
            Filter = IssueFilter.Assigned,
            State = ItemStateFilter.Open
        };

        var currentUser = await _gitHubClient.User.Current();
        var issues = await _gitHubClient.Issue.GetAllForOwnedAndMemberRepositories(request);

        return issues
            .Where(issue => issue.Assignees.Select(a => a.Id).Contains(currentUser.Id))
            .Select(issue => new WorkItem(issue))
            .ToList();
    }

    public async Task Label(WorkItem workItem, List<StateMachineLabel> addLabels, List<StateMachineLabel> removeLabels, CancellationToken cancellationToken)
    {
        foreach (var label in removeLabels)
        {
            try
            {
                await _gitHubClient.Issue.Labels.RemoveFromIssue(workItem.RepositoryOwner, workItem.RepositoryName, workItem.Id, label.ToLabelString());
            }
            catch (Octokit.NotFoundException)
            {
                // Ignore
            }
        }

        var labels = addLabels.Select(l => l.ToLabelString()).ToArray();
        await _gitHubClient.Issue.Labels.AddToIssue(workItem.RepositoryOwner, workItem.RepositoryName, workItem.Id, labels);
    }

    public async Task Label(WorkItem workItem, StateMachineLabel replacementLabel, CancellationToken cancellationToken)
    {
        var allStateMachineLabels = Enum.GetValues(typeof(StateMachineLabel)).Cast<StateMachineLabel>().Where(l => l != StateMachineLabel.Init).ToList();
        await Label(workItem, new List<StateMachineLabel> { replacementLabel }, allStateMachineLabels, cancellationToken);
    }

    public async Task EditComment(WorkItem workItem, int commentId, string comment, CancellationToken cancellationToken)
    {
        await _gitHubClient.Issue.Comment.Update(workItem.RepositoryOwner, workItem.RepositoryName, commentId, comment);
    }
}
using Octokit;

namespace Tarik.Application.Common;

public class WorkItem
{
    public int Id { get; }
    public string Title { get; }
    public string RepositoryName { get; }
    public string RepositoryOwner { get; }
    public string? Type { get; }
    public List<string> Labels { get; }
    public string State { get; }
    public string? AssignedTo { get; }
    public string Body { get; }
    public DateTime? UpdatedDate { get; }

    public WorkItem(Issue issue)
    {
        Id = issue.Number;
        Title = issue.Title;
        RepositoryName = issue.Repository.Name;
        RepositoryOwner = issue.Repository.Owner.Login;
        Labels = issue.Labels.Select(label => label.Name).ToList();
        State = issue.State.Value.ToString();
        Body = issue.Body;
        AssignedTo = issue.Assignee?.Name;
        UpdatedDate = issue.UpdatedAt.HasValue ? issue.UpdatedAt.Value.DateTime : null;
    }
}
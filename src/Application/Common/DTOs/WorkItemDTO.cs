using Octokit;

namespace Tarik.Application.Common.DTOs;

public class WorkItemDTO
{
    public int Id { get; }
    public string Title { get; }
    public string? Type { get; }
    public List<string> Labels { get; }
    public string State { get; }
    public string? AssignedTo { get; }
    public string Body { get; }
    public DateTime? UpdatedDate { get; }

    public WorkItemDTO(Issue issue)
    {
        Id = issue.Number;
        Title = issue.Title;
        Labels = issue.Labels.Select(label => label.Name).ToList();
        State = issue.State.Value.ToString();
        Body = issue.Body;
        AssignedTo = issue.Assignee?.Name;
        UpdatedDate = issue.UpdatedAt.HasValue ? issue.UpdatedAt.Value.DateTime : null;
    }
}
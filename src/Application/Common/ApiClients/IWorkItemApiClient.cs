

namespace Tarik.Application.Common;

/// <summary>
/// The interface for the API client that is used to retrieve work items, such as GitHub issues, Azure DevOps work items, etc.
/// </summary>
public interface IWorkItemApiClient
{
    Task<int> Comment(int workItemId, string comment, CancellationToken cancellationToken);
    public Task<List<WorkItem>> GetOpenWorkItems(CancellationToken cancellationToken);
    Task Label(int workItemId, List<StateMachineLabel> addLabels, List<StateMachineLabel> removeLabels, CancellationToken cancellationToken);
    Task Label(int workItemId, StateMachineLabel replacementLabel, CancellationToken cancellationToken);
    Task<List<Comment>> GetCommentsAsync(int id);
}
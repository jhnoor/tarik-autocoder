

namespace Tarik.Application.Common;

public interface IWorkItemService
{
    Task<int> Comment(int workItemId, string comment, CancellationToken cancellationToken);
    Task EditComment(int commentId, string comment, CancellationToken cancellationToken);
    public Task<List<WorkItem>> GetOpenWorkItems(CancellationToken cancellationToken);
    Task Label(int workItemId, List<StateMachineLabel> addLabels, List<StateMachineLabel> removeLabels, CancellationToken cancellationToken);
    Task Label(int workItemId, StateMachineLabel replacementLabel, CancellationToken cancellationToken);
    Task<List<Comment>> GetCommentsAsync(int id);
}
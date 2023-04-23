

namespace Tarik.Application.Common;

public interface IWorkItemService
{
    Task<int> Comment(WorkItem workItem, string comment, CancellationToken cancellationToken);
    Task EditComment(WorkItem workItem, int commentId, string comment, CancellationToken cancellationToken);
    Task<List<WorkItem>> GetIssuesAssignedToTarik(CancellationToken cancellationToken);
    Task Label(WorkItem workItem, List<StateMachineLabel> addLabels, List<StateMachineLabel> removeLabels, CancellationToken cancellationToken);
    Task Label(WorkItem workItem, StateMachineLabel replacementLabel, CancellationToken cancellationToken);
    Task<List<Comment>> GetCommentsAsync(WorkItem workItem);
}
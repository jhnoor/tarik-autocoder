using Tarik.Application.Common.DTOs;

namespace Tarik.Application.Common;

/// <summary>
/// The interface for the API client that is used to retrieve work items, such as GitHub issues, Azure DevOps work items, etc.
/// </summary>
public interface IWorkItemApiClient
{
    Task<int> Comment(int id, string comment, CancellationToken cancellationToken);
    public Task<List<WorkItemDTO>> GetOpenWorkItems(CancellationToken cancellationToken);
    Task LabelAwaitingPlanApproval(int id, CancellationToken cancellationToken);
    Task LabelAwaitingImplementation(int id, CancellationToken cancellationToken);
    Task LabelAwaitingCodeReview(int id, CancellationToken cancellationToken);
}
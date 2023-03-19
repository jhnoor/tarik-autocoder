using Microsoft.VisualStudio.Services.WebApi;

namespace Tarik.Application.Common.DTOs;

public class WorkItemUpdateDTO
{
    public int WorkItemId { get; }
    public string Title { get; }
    public string Type { get; }
    public string State { get; }
    public string AssignedTo { get; }
    public DateTime? UpdatedDate { get; }

    public WorkItemUpdateDTO(int workItemId, string title, string type, string state, IdentityRef assignedTo, DateTime? updatedDate)
    {
        WorkItemId = workItemId;
        Title = title;
        Type = type;
        State = state;
        AssignedTo = assignedTo.DisplayName;
        UpdatedDate = updatedDate;
    }
}
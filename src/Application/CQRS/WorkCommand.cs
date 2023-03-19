using MediatR;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class WorkCommand : IRequest<Unit>
{
    public WorkCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class WorkCommandHandler : IRequestHandler<WorkCommand>
    {
        private readonly ISender _mediator;
        private readonly IWorkItemApiClient _workItemApiClient;

        public WorkCommandHandler(ISender mediator, IWorkItemApiClient workItemApiClient)
        {
            _mediator = mediator;
            _workItemApiClient = workItemApiClient;
        }

        public async Task<Unit> Handle(WorkCommand request, CancellationToken cancellationToken)
        {
            var state = RetrieveStateFromLabels(request.WorkItem.Labels);

            switch (state)
            {
                case StateMachineLabel.AutoCodeInit:
                    var planWorkCommandResponse = await _mediator.Send(new PlanWorkCommand(request.WorkItem), cancellationToken);
                    var commentId = await _workItemApiClient.Comment(request.WorkItem.Id, planWorkCommandResponse.Response, cancellationToken);
                    await _workItemApiClient.LabelAwaitingPlanApproval(request.WorkItem.Id, cancellationToken);
                    break;

                default:
                    throw new ArgumentException($"Unknown state: {state}.");
            }

            return Unit.Value;
        }

        private StateMachineLabel? RetrieveStateFromLabels(List<string> labelStrings)
        {
            IEnumerable<StateMachineLabel> labels = labelStrings
                .Select(ls => ls.FromLabelString())
                .Where(l => l != null)
                .Cast<StateMachineLabel>();

            return labels.OrderDescending().FirstOrDefault();
        }
    }
}
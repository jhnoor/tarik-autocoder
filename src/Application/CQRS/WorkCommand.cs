using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<WorkCommandHandler> _logger;

        public WorkCommandHandler(ISender mediator, IWorkItemApiClient workItemApiClient, ILogger<WorkCommandHandler> logger)
        {
            _mediator = mediator;
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Unit> Handle(WorkCommand request, CancellationToken cancellationToken)
        {
            var state = RetrieveStateFromLabels(request.WorkItem.Labels);

            switch (state)
            {
                case StateMachineLabel.Init:
                    await _mediator.Send(new PlanWorkCommand(request.WorkItem), cancellationToken);
                    break;
                case StateMachineLabel.AutoCodeAwaitingImplementation:
                    throw new NotImplementedException();
                case StateMachineLabel.AutoCodeAwaitingCodeReview:
                    throw new NotImplementedException();
                case StateMachineLabel.AutoCodeAwaitingPlanApproval:
                    await _mediator.Send(new CheckPlanApprovalCommand(request.WorkItem), cancellationToken);
                    break;
                default:
                    throw new ArgumentException($"Unknown state: {state}");
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
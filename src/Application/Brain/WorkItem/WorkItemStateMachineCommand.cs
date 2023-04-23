using MediatR;
using Microsoft.Extensions.Logging;
using Tarik.Application.Common;

namespace Tarik.Application.Brain;

public class WorkItemStateMachineCommand : IRequest<Unit>
{
    public WorkItemStateMachineCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class WorkItemStateMachineCommandHandler : IRequestHandler<WorkItemStateMachineCommand>
    {
        private readonly ISender _mediator;
        private readonly ILogger<WorkItemStateMachineCommandHandler> _logger;

        public WorkItemStateMachineCommandHandler(
            ISender mediator,
            ILogger<WorkItemStateMachineCommandHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Unit> Handle(WorkItemStateMachineCommand request, CancellationToken cancellationToken)
        {
            var state = RetrieveStateFromLabels(request.WorkItem.Labels);

            switch (state)
            {
                case StateMachineLabel.Init:
                    await _mediator.Send(new UnderstandRepositoryCommand(request.WorkItem), cancellationToken);
                    break;
                case StateMachineLabel.AutoCodePlanning:
                    await _mediator.Send(new PlanWorkCommand(request.WorkItem), cancellationToken);
                    break;
                case StateMachineLabel.AutoCodeAwaitingImplementation:
                    await _mediator.Send(new ExecutePlanCommand(request.WorkItem), cancellationToken);
                    break;
                case StateMachineLabel.AutoCodeAwaitingCodeReview:
                    _logger.LogDebug($"Work item #{request.WorkItem.Id} has been implemented - Awaiting code review");
                    break;
                case StateMachineLabel.AutoCodeAwaitingPlanApproval:
                    await _mediator.Send(new CheckPlanApprovalCommand(request.WorkItem), cancellationToken);
                    break;
                case StateMachineLabel.AutoCodeFailExecution:
                    throw new NotImplementedException();
                case StateMachineLabel.AutoCodeFailPlanNotParsable:
                    throw new NotImplementedException();
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
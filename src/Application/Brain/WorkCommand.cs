using MediatR;
using Microsoft.Extensions.Logging;
using Tarik.Application.Common;


namespace Tarik.Application.Brain;

public class WorkCommand : IRequest<Unit>
{
    public WorkCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class WorkCommandHandler : IRequestHandler<WorkCommand>
    {
        private readonly ISender _mediator;
        private readonly IWorkItemService _workItemApiClient;
        private readonly ILogger<WorkCommandHandler> _logger;

        public WorkCommandHandler(ISender mediator, IWorkItemService workItemApiClient, ILogger<WorkCommandHandler> logger)
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
                    Plan plan = await _mediator.Send(new ParsePlanCommand(request.WorkItem), cancellationToken);
                    await _mediator.Send(new ExecutePlanCommand(request.WorkItem, plan), cancellationToken);
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
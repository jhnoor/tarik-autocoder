using MediatR;
using Microsoft.Extensions.Logging;
using Tarik.Application.Common;

namespace Tarik.Application.Brain;

public class ParsePlanCommand : IRequest<Plan>
{
    public ParsePlanCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class ParsePlanCommandHandler : IRequestHandler<ParsePlanCommand, Plan>
    {
        private readonly IWorkItemService _workItemApiClient;
        private readonly ILogger<ParsePlanCommandHandler> _logger;

        public ParsePlanCommandHandler(IWorkItemService workItemApiClient, ILogger<ParsePlanCommandHandler> logger)
        {
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Plan> Handle(ParsePlanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Parsing plan");

            try
            {
                var comments = await _workItemApiClient.GetCommentsAsync(request.WorkItem.Id);

                var approvedPlanComment = comments.FirstOrDefault(c => c.IsApprovedPlan);

                if (approvedPlanComment == null)
                {
                    throw new InvalidOperationException($"Invalid label state for work item {request.WorkItem.Id}, no approved plan comment found");
                }

                return new Plan(approvedPlanComment.Body);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to parse plan");
                await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeFailPlanNotParsable, cancellationToken);
                throw;
            }
        }
    }
}

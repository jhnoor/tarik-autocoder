using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using Tarik.Application.Common;


namespace Tarik.Application.Brain;

public class CheckPlanApprovalCommand : IRequest<Unit>
{
    public CheckPlanApprovalCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class CheckPlanApprovalCommandHandler : IRequestHandler<CheckPlanApprovalCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly ILogger<CheckPlanApprovalCommandHandler> _logger;

        public CheckPlanApprovalCommandHandler(IOpenAIService openAIService, IWorkItemService workItemApiClient, ILogger<CheckPlanApprovalCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Unit> Handle(CheckPlanApprovalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Checking plan approval for work item {request.WorkItem.Id}");

            var comments = await _workItemApiClient.GetCommentsAsync(request.WorkItem.Id);

            var approvedPlanComment = comments.FirstOrDefault(c => c.IsLiked && c.IsTarik);

            if (approvedPlanComment == null)
            {
                _logger.LogDebug($"No approved plan comment found for work item {request.WorkItem.Id}");
                return Unit.Value;
            }

            _logger.LogDebug($"Found approved plan comment: {approvedPlanComment.Url}");
            await _workItemApiClient.EditComment(approvedPlanComment.Id, $"{approvedPlanComment.Body}\n\n EDIT: Plan approved! ✅", cancellationToken);
            await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeAwaitingImplementation, cancellationToken);
            return Unit.Value;
        }
    }
}
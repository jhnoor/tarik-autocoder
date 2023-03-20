using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class CheckPlanApprovalCommand : IRequest<Unit>
{
    public CheckPlanApprovalCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class CheckPlanApprovalCommandHandler : IRequestHandler<CheckPlanApprovalCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemApiClient _workItemApiClient;
        private readonly ILogger<CheckPlanApprovalCommandHandler> _logger;

        public CheckPlanApprovalCommandHandler(IOpenAIService openAIService, IWorkItemApiClient workItemApiClient, ILogger<CheckPlanApprovalCommandHandler> logger)
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
            await _workItemApiClient.LabelAwaitingImplementation(request.WorkItem.Id, approvedPlanComment, cancellationToken);

            return Unit.Value;
        }
    }
}
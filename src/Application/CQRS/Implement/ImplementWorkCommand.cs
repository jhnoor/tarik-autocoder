using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class ImplementWorkCommand : IRequest<Unit>
{
    public ImplementWorkCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class ImplementWorkCommandHandler : IRequestHandler<ImplementWorkCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemApiClient _workItemApiClient;
        private readonly ILogger<ImplementWorkCommandHandler> _logger;

        public ImplementWorkCommandHandler(IOpenAIService openAIService, IWorkItemApiClient workItemApiClient, ILogger<ImplementWorkCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Unit> Handle(ImplementWorkCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id}");

            var comments = await _workItemApiClient.GetCommentsAsync(request.WorkItem.Id);

            var approvedPlanComment = comments.FirstOrDefault(c => c.IsApprovedPlan);

            if (approvedPlanComment == null)
            {
                throw new InvalidOperationException($"Invalid label state for work item {request.WorkItem.Id}, no approved plan comment found");
            }

            // TODO: Implement work item according to plan
            throw new NotImplementedException("Implement work item according to plan");

            return Unit.Value;
        }
    }
}
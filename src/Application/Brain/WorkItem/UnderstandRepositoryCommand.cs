using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using Polly;
using Tarik.Application.Common;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class UnderstandRepositoryCommand : IRequest<Unit>
{
    public UnderstandRepositoryCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class UnderstandRepositoryCommandHandler : IRequestHandler<UnderstandRepositoryCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IFileServiceFactory _fileServiceFactory;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<UnderstandRepositoryCommandHandler> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public UnderstandRepositoryCommandHandler(
            IOpenAIService openAIService,
            IWorkItemService workItemApiClient,
            IFileServiceFactory fileServiceFactory,
            IShortTermMemoryService shortTermMemoryService,
            ILogger<UnderstandRepositoryCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _fileServiceFactory = fileServiceFactory;
            _shortTermMemoryService = shortTermMemoryService;
            _logger = logger;
            _retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
        }

        public async Task<Unit> Handle(UnderstandRepositoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Understanding repository {request.WorkItem.RepositoryOwner}/{request.WorkItem.RepositoryName} for work item {request.WorkItem.Id}");
            IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);

            List<PathTo> paths = fileService.GetPaths();

            foreach (var path in paths)
            {
                await _shortTermMemoryService.Memorize(
                    request.WorkItem.RepositoryOwner,
                    request.WorkItem.RepositoryName,
                    path,
                    cancellationToken);
            }

            await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodePlanning, cancellationToken);

            return Unit.Value;
        }
    }
}
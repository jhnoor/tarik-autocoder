using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly;
using Tarik.Application.Common;
using TiktokenSharp;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class PlanWorkCommand : IRequest<Unit>
{
    public PlanWorkCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class PlanWorkCommandHandler : IRequestHandler<PlanWorkCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IFileServiceFactory _fileServiceFactory;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<PlanWorkCommandHandler> _logger;

        public PlanWorkCommandHandler(
            IOpenAIService openAIService,
            IWorkItemService workItemApiClient,
            IFileServiceFactory fileServiceFactory,
            IShortTermMemoryService shortTermMemoryService,
            ILogger<PlanWorkCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _fileServiceFactory = fileServiceFactory;
            _shortTermMemoryService = shortTermMemoryService;
            _logger = logger;
        }

        public async Task<Unit> Handle(PlanWorkCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Planning work for work item {request.WorkItem.Id}");
            IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);
            string shortTermMemory = _shortTermMemoryService.Dump();
            string planningPrompt = request.WorkItem.GetPlanningPrompt(shortTermMemory);
            IAsyncPolicy retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);

            _logger.LogDebug($"Planning prompt: {planningPrompt}");

            TikToken tikToken = TikToken.EncodingForModel("gpt-4");
            int promptTokenLength = tikToken.Encode(planningPrompt).Count;
            _logger.LogDebug($"Planning prompt token length: {promptTokenLength}");

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt_4,
                MaxTokens = 8000 - promptTokenLength,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(planningPrompt),
                }
            };

            _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");

            var chatResponse = await retryPolicy
                .ExecuteAsync(async () => await _openAIService.ChatCompletion
                    .CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken)
                );

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            _logger.LogDebug($"OpenAI content: {chatResponse.Choices.First().Message.Content}");
            var commentId = await _workItemApiClient.Comment(request.WorkItem, chatResponse.Choices.First().Message.Content, cancellationToken);
            await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodeAwaitingPlanApproval, cancellationToken);

            return Unit.Value;
        }
    }
}
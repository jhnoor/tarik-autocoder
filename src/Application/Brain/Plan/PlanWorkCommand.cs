using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;


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
        private readonly Common.IFileService _fileService;
        private readonly ILogger<PlanWorkCommandHandler> _logger;

        public PlanWorkCommandHandler(IOpenAIService openAIService, IWorkItemService workItemApiClient, Common.IFileService fileService, ILogger<PlanWorkCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Unit> Handle(PlanWorkCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Planning work for work item {request.WorkItem.Id}");
            string tree = await _fileService.Tree(cancellationToken: cancellationToken);
            string planningPrompt = request.WorkItem.GetPlanningPrompt(tree);
            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt4,
                MaxTokens = 8000 - planningPrompt.Length,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(planningPrompt),
                }
            };

            _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");
            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            var aiPlan = chatResponse.Choices.First().Message.Content;
            // Trim ```md from the start and ``` from the end
            aiPlan = aiPlan.Trim('`').Trim('m').Trim('d').Trim();

            _logger.LogDebug($"AI response: {chatResponse.Choices.First().Message.Content}");
            var commentId = await _workItemApiClient.Comment(request.WorkItem.Id, chatResponse.Choices.First().Message.Content, cancellationToken);
            await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeAwaitingPlanApproval, cancellationToken);

            return Unit.Value;
        }
    }
}
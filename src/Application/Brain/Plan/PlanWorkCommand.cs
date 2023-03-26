using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class PlanWorkCommand : IRequest<Unit>
{
    public PlanWorkCommand(WorkItem workItem, IServiceScope scope)
    {
        WorkItem = workItem;
        Scope = scope;
    }

    public WorkItem WorkItem { get; }
    public IServiceScope Scope { get; }

    public class PlanWorkCommandHandler : IRequestHandler<PlanWorkCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly ILogger<PlanWorkCommandHandler> _logger;

        public PlanWorkCommandHandler(IOpenAIService openAIService, IWorkItemService workItemApiClient, ILogger<PlanWorkCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Unit> Handle(PlanWorkCommand request, CancellationToken cancellationToken)
        {
            IFileService fileService = request.Scope.ServiceProvider.GetRequiredService<IFileService>();
            _logger.LogDebug($"Planning work for work item {request.WorkItem.Id}");
            string paths = fileService.GetPaths();
            string planningPrompt = request.WorkItem.GetPlanningPrompt(paths);
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

            _logger.LogDebug($"AI response: {chatResponse.Choices.First().Message.Content}");
            var commentId = await _workItemApiClient.Comment(request.WorkItem.Id, chatResponse.Choices.First().Message.Content, cancellationToken);
            await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeAwaitingPlanApproval, cancellationToken);

            return Unit.Value;
        }
    }
}
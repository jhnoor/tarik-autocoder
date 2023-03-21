using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;

namespace Tarik.Application.CQRS;

public class ImplementWorkCommand : IRequest<Unit>
{
    public ImplementWorkCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

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

            var prompt = @"
Only respond with JSON. Convert the given plan into JSON of this format:

{
    ""CreateFileSteps"": [
        {
            ""Path"": ""<path>"",
            ""Reason"": ""<reason>""
        }
    ],
    ""EditFileSteps"": [
        {
            ""Path"": ""<path>"",
            ""Reason"": ""<reason>""
        }
    ],
    ""DeleteFileSteps"": [
        {
            ""Path"": ""<path>"",
            ""Reason"": ""<reason>""
        }
    ]
}
            ";

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 3500,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(prompt),
                        ChatMessage.FromUser(approvedPlanComment.Body)
                    }
            };

            _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");
            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to parse plan: {chatResponse.Error?.Message}");
            }

            _logger.LogDebug($"AI response: {chatResponse.Choices.First().Message.Content}");

            var plan = JsonSerializer.Deserialize<Plan>(chatResponse.Choices.First().Message.Content);

            throw new NotImplementedException("Execute plan");
        }
    }
}
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class PlanWorkCommand : IRequest<Unit>
{
    public PlanWorkCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class PlanWorkCommandHandler : IRequestHandler<PlanWorkCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemApiClient _workItemApiClient;
        private readonly ILogger<PlanWorkCommandHandler> _logger;

        public PlanWorkCommandHandler(IOpenAIService openAIService, IWorkItemApiClient workItemApiClient, ILogger<PlanWorkCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _logger = logger;
        }

        public async Task<Unit> Handle(PlanWorkCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Planning work for work item {request.WorkItem.Id}");
            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt4,
                MaxTokens = 4000,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(@"
Your name is Tarik, you are a bot that writes code. In this conversation you will be planning your work. 
You will receive a work item from your manager, and you will need to plan how you will work on it.
There may include a message directly you prefixed with @tarik-tasktopr. Your response must be in markdown.

In your plan, you can ONLY use the following commands:

* Create a new file <filename>
* Edit the file <filename>
* Delete the file <filename>
* Handover these tasks: <task1>, <task2>, <task3>

Handover tasks are tasks that you will handover to another more senior developer, and you will not be working on them.
You should only handover tasks that cannot be completed by the just creating, editing or deleting a file. 
Make sure you are not handing over tasks that you can complete yourself.

The work item you receive will be in the following format:

> Title: [Work Item Title]
> Description: [Work Item Description]

Here's an example:

> Title: Add an emoji to our README.md file
> Description: We need to add an emoji to the end of our README.md file.

Your response should be in the following format:

## Plan

1. Edit the file README.md - in order to add an emoji to the end of the file

"),
                }
            };

            var formattedWorkItem = @$" 
> Title: {request.WorkItem.Title}{Environment.NewLine}
> Description: {request.WorkItem.Body}";

            chatCompletionCreateRequest.Messages.Add(ChatMessage.FromUser(formattedWorkItem));

            _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");
            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            _logger.LogDebug($"AI response: {chatResponse.Choices.First().Message.Content}");
            var commentId = await _workItemApiClient.Comment(request.WorkItem.Id, chatResponse.Choices.First().Message.Content, cancellationToken);
            await _workItemApiClient.LabelAwaitingPlanApproval(request.WorkItem.Id, cancellationToken);

            return Unit.Value;
        }
    }
}
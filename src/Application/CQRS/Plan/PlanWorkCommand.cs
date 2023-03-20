using MediatR;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class PlanWorkCommand : IRequest<PlanWorkCommandResponse>
{
    public PlanWorkCommand(WorkItemDTO workItem)
    {
        WorkItem = workItem;
    }

    public WorkItemDTO WorkItem { get; }

    public class PlanWorkCommandHandler : IRequestHandler<PlanWorkCommand, PlanWorkCommandResponse>
    {
        private readonly IOpenAIService _openAIService;

        public PlanWorkCommandHandler(IOpenAIService openAIService)
        {
            _openAIService = openAIService;
        }

        public async Task<PlanWorkCommandResponse> Handle(PlanWorkCommand request, CancellationToken cancellationToken)
        {
            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt4,
                MaxTokens = 3000,
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

            string formattedWorkItem = @$" 
> Title: {request.WorkItem.Title}{Environment.NewLine}
> Description: {request.WorkItem.Body}";

            chatCompletionCreateRequest.Messages.Add(ChatMessage.FromUser(formattedWorkItem));

            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            return new PlanWorkCommandResponse(chatResponse);
        }
    }
}
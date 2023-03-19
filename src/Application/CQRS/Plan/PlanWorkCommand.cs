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
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 3500,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(@"
Your name is TarikPlanner - a bot that helps you plan your work. 
You will receive a work item from your manager, and you will need to plan how you will work on it.
The message will look like this:

> Title: [Work Item Title]
> Description: [Work Item Description]

You will respond with a step-by-step plan of how you will work on the work item formatted in markdown. This plan will be sent to your manager for approval.
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
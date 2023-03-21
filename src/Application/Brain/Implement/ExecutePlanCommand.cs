using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;

namespace Tarik.Application.Brain;

public class ExecutePlanCommand : IRequest<Unit>
{
    public ExecutePlanCommand(WorkItem workItem, Plan plan)
    {
        WorkItem = workItem;
        Plan = plan;
    }

    public WorkItem WorkItem { get; }
    public Plan Plan { get; }

    public class ExecutePlanCommandHandler : IRequestHandler<ExecutePlanCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemApiClient _workItemApiClient;
        private readonly Common.IFileService _fileService;
        private readonly ILogger<ExecutePlanCommandHandler> _logger;

        public ExecutePlanCommandHandler(IOpenAIService openAIService, IWorkItemApiClient workItemApiClient, Common.IFileService fileService, ILogger<ExecutePlanCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecutePlanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Executing plan");

            try
            {
                var branchName = $"tarik/{request.WorkItem.Id}-{request.WorkItem.Title.ToLower().Replace(' ', '-')}";
                await _fileService.CreateBranch(branchName, cancellationToken);

                foreach (var createFileStep in request.Plan.CreateFileSteps)
                {
                    if (createFileStep.Path == null)
                        throw new ArgumentException("Path is required for CreateFilePlanStep");

                    await _fileService.CreateFile(createFileStep.Path, "<NOTHING>", branchName, cancellationToken);
                }

                var rootTree = await _fileService.Tree("/", cancellationToken);
                foreach (var editFileStep in request.Plan.EditFileSteps)
                {
                    if (editFileStep.Path == null)
                        throw new ArgumentException("Path is required for EditFilePlanStep");

                    editFileStep.CurrentContent = await _fileService.GetFileContent(editFileStep.Path, branchName, cancellationToken);
                    editFileStep.AISuggestedContent = await GenerateContent(editFileStep, rootTree, cancellationToken);

                    await _fileService.EditFile(editFileStep.Path, editFileStep.AISuggestedContent, branchName, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing plan for work item {request.WorkItem.Id}");
                await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeFailExecution, cancellationToken);
                throw;
            }

            return Unit.Value;
        }

        private async Task<string> GenerateContent(EditFilePlanStep editStep, string rootTree, CancellationToken cancellationToken)
        {

            var prompt = $@"
You are Tarik, a senior software developer. You are given a task to edit a file. The file is located at:

- {editStep.Path}

This is the current content of the file:

```
{editStep.CurrentContent}
``` 

This is the tree view of the repository:
```
{rootTree}
``` 

Make a very good guess at what the content of the file should look like. Respond with only the content.
            ";

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt4,
                MaxTokens = 8000,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(prompt),
                    }
            };

            _logger.LogDebug($"Sending file generation request to OpenAI: {chatCompletionCreateRequest}");
            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            var content = chatResponse.Choices.First().Message.Content;
            _logger.LogDebug($"AI response: {content}");
            return content;
        }
    }
}

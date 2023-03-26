using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class ExecutePlanCommand : IRequest<Unit>
{
    public ExecutePlanCommand(WorkItem workItem, Plan plan, IServiceScope scope)
    {
        WorkItem = workItem;
        Plan = plan;
        Scope = scope;
    }

    public WorkItem WorkItem { get; }
    public Plan Plan { get; }
    public IServiceScope Scope { get; }

    public class ExecutePlanCommandHandler : IRequestHandler<ExecutePlanCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IPullRequestService _pullRequestService;
        private readonly ILogger<ExecutePlanCommandHandler> _logger;

        public ExecutePlanCommandHandler(IOpenAIService openAIService, IWorkItemService workItemApiClient, IPullRequestService pullRequestService, ILogger<ExecutePlanCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _pullRequestService = pullRequestService;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecutePlanCommand request, CancellationToken cancellationToken)
        {
            IFileService fileService = request.Scope.ServiceProvider.GetRequiredService<IFileService>();
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Executing plan");
            var branchName = $"tarik/{request.WorkItem.Id}-{request.WorkItem.Title.ToLower().Replace(' ', '-')}";

            try
            {
                var sourceBranch = await fileService.CreateBranch(branchName, cancellationToken);

                foreach (var createFileStep in request.Plan.CreateFileSteps)
                {
                    if (createFileStep.Path == null)
                        throw new ArgumentException("Path is required for CreateFilePlanStep");

                    await fileService.CreateFile(createFileStep.Path, "<NOTHING>", sourceBranch, cancellationToken);
                }

                var paths = fileService.GetPaths();
                foreach (var editFileStep in request.Plan.EditFileSteps)
                {
                    if (editFileStep.Path == null)
                        throw new ArgumentException("Path is required for EditFilePlanStep");

                    editFileStep.CurrentContent = await fileService.GetFileContent(editFileStep.Path, sourceBranch, cancellationToken);
                    editFileStep.AISuggestedContent = await GenerateContent(editFileStep, paths, cancellationToken);

                    await fileService.EditFile(editFileStep.Path, editFileStep.AISuggestedContent, sourceBranch, cancellationToken);
                }

                await _pullRequestService.CreatePullRequest(request.WorkItem, sourceBranch.Ref, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing plan for work item {request.WorkItem.Id}");
                await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeFailExecution, cancellationToken);
                throw;
            }

            _logger.LogDebug($"Branch {branchName} created and updated for work item {request.WorkItem.Id}");
            await _workItemApiClient.Label(request.WorkItem.Id, StateMachineLabel.AutoCodeAwaitingCodeReview, cancellationToken);

            return Unit.Value;
        }

        private async Task<string> GenerateContent(EditFilePlanStep editStep, string paths, CancellationToken cancellationToken)
        {
            var prompt = editStep.GetEditFileStepPrompt(paths);

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.Gpt4,
                MaxTokens = 8000 - prompt.Length,
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

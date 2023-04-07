using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly;
using Tarik.Application.Common;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class ExecutePlanCommand : IRequest<Unit>
{
    public ExecutePlanCommand(WorkItem workItem, Plan plan, IFileService fileService)
    {
        WorkItem = workItem;
        Plan = plan;
        FileService = fileService;
    }

    public WorkItem WorkItem { get; }
    public Plan Plan { get; }
    public IFileService FileService { get; }

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
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Executing plan");
            var branchName = $"tarik/{request.WorkItem.Id}-{request.WorkItem.Title.ToLower().Replace(' ', '-')}";
            var retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);

            try
            {
                var sourceBranch = await request.FileService.CreateBranch(branchName, cancellationToken);
                var paths = request.FileService.GetPaths();

                foreach (var createFileStep in request.Plan.CreateFileSteps)
                {
                    if (createFileStep.Path == null)
                        throw new ArgumentException("Path is required for CreateFilePlanStep");

                    var context = new Context { ["RetryCount"] = 0 };
                    createFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(createFileStep, request.Plan.StepByStepDiscussion, paths, (int)ctx["RetryCount"], cancellationToken), new Context { ["RetryCount"] = 0 });

                    await request.FileService.CreateFile(createFileStep, sourceBranch, cancellationToken);
                }

                foreach (var editFileStep in request.Plan.EditFileSteps)
                {
                    if (editFileStep.Path == null)
                        throw new ArgumentException("Path is required for EditFilePlanStep");

                    editFileStep.CurrentContent = await request.FileService.GetFileContent(editFileStep.Path, sourceBranch, cancellationToken);

                    var context = new Context { ["RetryCount"] = 0 };
                    editFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(editFileStep, request.Plan.StepByStepDiscussion, paths, (int)ctx["RetryCount"], cancellationToken), context);

                    await request.FileService.EditFile(editFileStep, sourceBranch, cancellationToken);
                }

                await _pullRequestService.CreatePullRequest(request.WorkItem, sourceBranch.Ref, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing plan for work item {request.WorkItem.Id}");
                await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodeFailExecution, cancellationToken);
                throw;
            }

            _logger.LogDebug($"Branch {branchName} created and updated for work item {request.WorkItem.Id}");
            await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodeAwaitingCodeReview, cancellationToken);

            return Unit.Value;
        }

        private async Task<string> GenerateContent(MutateFilePlanStep mutateStep, string stepByStepDiscussion, string paths, int retryAttempt, CancellationToken cancellationToken)
        {
            var prompt = mutateStep switch
            {
                CreateFilePlanStep createStep => createStep.GetCreateFileStepPrompt(stepByStepDiscussion, paths),
                EditFilePlanStep editStep => editStep.GetEditFileStepPrompt(stepByStepDiscussion, paths),
                _ => throw new ArgumentException($"Only generate content for {nameof(CreateFilePlanStep)} and {nameof(EditFilePlanStep)}")
            };

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(prompt),
                }
            };

            if (retryAttempt > 1)
            {
                chatCompletionCreateRequest.Model = Models.ChatGpt3_5Turbo;
                chatCompletionCreateRequest.MaxTokens = 4000 - prompt.Length;
            }
            else
            {
                chatCompletionCreateRequest.Model = Models.Gpt_4;
                chatCompletionCreateRequest.MaxTokens = 8000 - prompt.Length;
            }

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

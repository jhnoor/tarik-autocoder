using System.Text.RegularExpressions;
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

public class ExecutePlanCommand : IRequest<Unit>
{
    public ExecutePlanCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class ExecutePlanCommandHandler : IRequestHandler<ExecutePlanCommand>
    {
        private static Regex removeMarkDownCodeBlockRegex = new Regex(@"^```[\s\S]*?\n|\n```$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IPullRequestService _pullRequestService;
        private readonly IFileServiceFactory _fileServiceFactory;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<ExecutePlanCommandHandler> _logger;

        public ExecutePlanCommandHandler(
            IOpenAIService openAIService,
            IWorkItemService workItemApiClient,
            IPullRequestService pullRequestService,
            IFileServiceFactory fileServiceFactory,
            IShortTermMemoryService shortTermMemoryService,
            ILogger<ExecutePlanCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _pullRequestService = pullRequestService;
            _fileServiceFactory = fileServiceFactory;
            _shortTermMemoryService = shortTermMemoryService;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecutePlanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Executing plan");
            try
            {
                IAsyncPolicy retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
                IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);
                string branchName = await fileService.BranchName(cancellationToken);
                Plan plan = await ParsePlan(request.WorkItem, cancellationToken);
                string shortTermMemory = _shortTermMemoryService.Dump();

                foreach (var createFileStep in plan.CreateFileSteps)
                {
                    if (createFileStep.Path == null)
                        throw new ArgumentException("Path is required for CreateFilePlanStep");

                    var context = new Context { ["RetryCount"] = 0 };
                    createFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(createFileStep, plan.StepByStepDiscussion, shortTermMemory, (int)ctx["RetryCount"], cancellationToken), new Context { ["RetryCount"] = 0 });

                    await fileService.CreateFile(createFileStep, cancellationToken);
                }

                foreach (var editFileStep in plan.EditFileSteps)
                {
                    if (editFileStep.Path == null)
                        throw new ArgumentException("Path is required for EditFilePlanStep");

                    editFileStep.CurrentContent = await fileService.GetFileContent(editFileStep.Path, cancellationToken);

                    var context = new Context { ["RetryCount"] = 0 };
                    editFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(editFileStep, plan.StepByStepDiscussion, shortTermMemory, (int)ctx["RetryCount"], cancellationToken), context);

                    await fileService.EditFile(editFileStep, cancellationToken);
                }

                await fileService.Push(cancellationToken);
                await _pullRequestService.CreatePullRequest(request.WorkItem, branchName, cancellationToken);

                _logger.LogDebug($"Branch {branchName} created and updated for work item {request.WorkItem.Id}");
                await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodeAwaitingCodeReview, cancellationToken);

                return Unit.Value;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing plan for work item {request.WorkItem.Id}");
                await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodeFailExecution, cancellationToken);
                throw;
            }
        }

        private async Task<string> GenerateContent(MutateFilePlanStep mutateStep, string stepByStepDiscussion, string shortTermMemory, int retryAttempt, CancellationToken cancellationToken)
        {
            var prompt = mutateStep switch
            {
                CreateFilePlanStep createStep => createStep.GetCreateFileStepPrompt(stepByStepDiscussion, shortTermMemory),
                EditFilePlanStep editStep => editStep.GetEditFileStepPrompt(stepByStepDiscussion, shortTermMemory),
                _ => throw new ArgumentException($"Only generate content for {nameof(CreateFilePlanStep)} and {nameof(EditFilePlanStep)}")
            };

            TikToken tikToken = TikToken.EncodingForModel("gpt-4");
            int promptTokenLength = tikToken.Encode(prompt).Count;

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
                chatCompletionCreateRequest.MaxTokens = 4000 - promptTokenLength;
            }
            else
            {
                chatCompletionCreateRequest.Model = Models.Gpt_4;
                chatCompletionCreateRequest.MaxTokens = 8000 - promptTokenLength;
            }

            _logger.LogDebug($"Sending file generation request to OpenAI: {chatCompletionCreateRequest}");
            var chatResponse = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            string content = removeMarkDownCodeBlockRegex.Replace(chatResponse.Choices.First().Message.Content, string.Empty);
            _logger.LogDebug($"AI response: {content}");
            return content;
        }

        private async Task<Plan> ParsePlan(WorkItem workItem, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {workItem.Id} - Parsing plan");

            try
            {
                var comments = await _workItemApiClient.GetCommentsAsync(workItem);

                var approvedPlanComment = comments.FirstOrDefault(c => c.IsApprovedPlan);

                if (approvedPlanComment == null)
                {
                    throw new InvalidOperationException($"Invalid label state for work item {workItem.Id}, no approved plan comment found");
                }

                return new Plan(approvedPlanComment.Body);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to parse plan");
                await _workItemApiClient.Label(workItem, StateMachineLabel.AutoCodeFailPlanNotParsable, cancellationToken);
                throw;
            }
        }
    }
}

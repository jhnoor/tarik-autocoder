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
                IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);
                IAsyncPolicy retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
                string branchName = await fileService.BranchName(cancellationToken);
                string localDirectory = fileService.LocalDirectory();
                Plan plan = await ParsePlan(request.WorkItem, localDirectory, cancellationToken);
                string shortTermMemory = _shortTermMemoryService.Dump();

                foreach (var createFileStep in plan.CreateFileSteps)
                {
                    var context = new Context { ["RetryCount"] = 0 };
                    createFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(createFileStep, plan.StepByStepDiscussion, shortTermMemory, (int)ctx["RetryCount"], cancellationToken), new Context { ["RetryCount"] = 0 });

                    await fileService.CreateFile(createFileStep, cancellationToken);
                    await _shortTermMemoryService.Memorize(createFileStep.PathTo, cancellationToken);
                }

                foreach (var editFileStep in plan.EditFileSteps)
                {
                    editFileStep.CurrentContent = await fileService.GetFileContent(editFileStep.PathTo, cancellationToken);

                    var context = new Context { ["RetryCount"] = 0 };
                    editFileStep.AISuggestedContent = await retryPolicy
                        .ExecuteAsync(async (ctx) => await GenerateContent(editFileStep, plan.StepByStepDiscussion, shortTermMemory, (int)ctx["RetryCount"], cancellationToken), context);

                    await fileService.EditFile(editFileStep, cancellationToken);
                    await _shortTermMemoryService.Memorize(editFileStep.PathTo, cancellationToken);
                }

                await fileService.Push(cancellationToken);
                await _pullRequestService.CreatePullRequest(request.WorkItem, branchName, cancellationToken);

                _logger.LogInformation($"Branch {branchName} created and updated for work item {request.WorkItem.Id}");
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
                Temperature = 0f,
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
                chatCompletionCreateRequest.Model = Models.Gpt_4; // TODO - change to GPT-4
                chatCompletionCreateRequest.MaxTokens = 8000 - promptTokenLength; // TODO - change to 8000
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

        private async Task<Plan> ParsePlan(WorkItem workItem, string localDirectory, CancellationToken cancellationToken)
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

                return new Plan(approvedPlanComment.Body, localDirectory);
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

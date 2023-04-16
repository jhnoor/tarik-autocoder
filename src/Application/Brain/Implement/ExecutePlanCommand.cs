using System.Text.Json;
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
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IPullRequestService _pullRequestService;
        private readonly IFileServiceFactory _fileServiceFactory;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<ExecutePlanCommandHandler> _logger;
        private readonly IAsyncPolicy _retryPolicy;

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
            _retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
        }

        public async Task<Unit> Handle(ExecutePlanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Implementing work item {request.WorkItem.Id} - Executing plan");
            try
            {
                IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);
                string branchName = await fileService.BranchName(cancellationToken);
                string localDirectory = fileService.LocalDirectory();
                Plan plan = await ParsePlan(request.WorkItem, localDirectory, cancellationToken);
                string shortTermMemory = _shortTermMemoryService.Dump();

                List<MutateFilePlanStep> initialPassMutateSteps = plan.EditFileSteps
                    .OfType<MutateFilePlanStep>()
                    .Concat(plan.CreateFileSteps.OfType<MutateFilePlanStep>())
                    .ToList();

                Dictionary<PathTo, List<RelevantFile>> mutatedFiles = new();

                // First pass
                foreach (var mutateStep in initialPassMutateSteps)
                {
                    var generation = await Mutate(mutateStep, plan, fileService, shortTermMemory, cancellationToken);

                    if (generation == null)
                    {
                        _logger.LogInformation($"No generation for {mutateStep.PathTo.RelativePath}");
                        continue;
                    }

                    mutatedFiles.Add(mutateStep.PathTo, generation.RelevantFiles);
                }

                var secondPassMutateSteps = mutatedFiles
                    .SelectMany(mutatedFile =>
                    {
                        return mutatedFile.Value
                            .Select(relevantFile => new EditFilePlanStep(
                                relevantFile.Path,
                                localDirectory,
                                $"Due to change in {mutatedFile.Key.RelativePath}. {relevantFile.Reason}",
                                new List<string> { relevantFile.Path }));
                    }).ToList();

                // Second pass
                foreach (var mutateStep in secondPassMutateSteps)
                {
                    await Mutate(mutateStep, plan, fileService, shortTermMemory, cancellationToken);
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

        private async Task<Generation?> Mutate(MutateFilePlanStep mutateStep, Plan plan, IFileService fileService, string shortTermMemory, CancellationToken cancellationToken)
        {
            var context = new Context { ["RetryCount"] = 0 };
            var generation = await _retryPolicy
                .ExecuteAsync(async (ctx) => await GenerateContent(mutateStep, plan, fileService, shortTermMemory, (int)ctx["RetryCount"], cancellationToken), new Context { ["RetryCount"] = 0 });

            if (generation.Content == null)
            {
                return null;
            }

            mutateStep.AISuggestedContent = generation.Content;

            if (mutateStep is CreateFilePlanStep createFileStep)
            {
                await fileService.CreateFile(createFileStep, cancellationToken);
            }
            else if (mutateStep is EditFilePlanStep editFileStep)
            {
                await fileService.EditFile(editFileStep, cancellationToken);
            }

            await _shortTermMemoryService.Memorize(mutateStep.PathTo, cancellationToken);
            return generation;
        }

        private async Task<Generation> GenerateContent(MutateFilePlanStep mutateStep, Plan plan, IFileService fileService, string shortTermMemory, int retryAttempt, CancellationToken cancellationToken)
        {
            var prompt = mutateStep switch
            {
                CreateFilePlanStep createStep => await createStep.GetCreateFileStepPrompt(plan, fileService, shortTermMemory, cancellationToken),
                EditFilePlanStep editStep => await editStep.GetEditFileStepPrompt(plan, fileService, shortTermMemory, cancellationToken),
                _ => throw new ArgumentException($"Only generate content for {nameof(CreateFilePlanStep)} and {nameof(EditFilePlanStep)}")
            };

            _logger.LogDebug($"Prompt: {prompt}");

            TikToken tikToken = TikToken.EncodingForModel("gpt-4");
            int promptTokenLength = tikToken.Encode(prompt).Count;

            _logger.LogDebug($"Prompt token length: {promptTokenLength} (max 8000)");

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
            var content = chatResponse.Choices.First().Message.Content;
            Generation generation = content.Deserialize<Generation>() ?? throw new InvalidOperationException("Failed to deserialize OpenAI response");
            _logger.LogDebug(generation.ToString());

            return generation;
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

                _logger.LogDebug($"Found approved plan for work item {workItem.Id}");
                _logger.LogDebug(approvedPlanComment.Body);

                return new Plan(approvedPlanComment.Body, workItem.Title, workItem.Body, localDirectory);
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

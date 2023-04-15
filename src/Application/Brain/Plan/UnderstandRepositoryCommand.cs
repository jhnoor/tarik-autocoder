using System.Security.Cryptography;
using System.Text;
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

public class UnderstandRepositoryCommand : IRequest<Unit>
{
    public UnderstandRepositoryCommand(WorkItem workItem)
    {
        WorkItem = workItem;
    }

    public WorkItem WorkItem { get; }

    public class UnderstandRepositoryCommandHandler : IRequestHandler<UnderstandRepositoryCommand>
    {
        private readonly IOpenAIService _openAIService;
        private readonly IWorkItemService _workItemApiClient;
        private readonly IFileServiceFactory _fileServiceFactory;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<UnderstandRepositoryCommandHandler> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public UnderstandRepositoryCommandHandler(
            IOpenAIService openAIService,
            IWorkItemService workItemApiClient,
            IFileServiceFactory fileServiceFactory,
            IShortTermMemoryService shortTermMemoryService,
            ILogger<UnderstandRepositoryCommandHandler> logger)
        {
            _openAIService = openAIService;
            _workItemApiClient = workItemApiClient;
            _fileServiceFactory = fileServiceFactory;
            _shortTermMemoryService = shortTermMemoryService;
            _logger = logger;
            _retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
        }

        public async Task<Unit> Handle(UnderstandRepositoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Understanding repository {request.WorkItem.RepositoryOwner}/{request.WorkItem.RepositoryName} for work item {request.WorkItem.Id}");
            IFileService fileService = _fileServiceFactory.CreateFileService(request.WorkItem);

            List<PathTo> paths = fileService.GetPaths();

            foreach (var path in paths)
            {
                await Memorize(path, cancellationToken);
            }

            await _workItemApiClient.Label(request.WorkItem, StateMachineLabel.AutoCodePlanning, cancellationToken);

            return Unit.Value;
        }

        private async Task Memorize(PathTo path, CancellationToken cancellationToken)
        {
            // using md5 get hash of file in path
            using var md5 = MD5.Create();

            string content = await File.ReadAllTextAsync(path.AbsolutePath, cancellationToken);
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
            string hashString = Convert.ToBase64String(hash);

            if (_shortTermMemoryService.Recall(path, hashString) == null)
            {
                _logger.LogInformation($"File {path} has changed, summarizing");
                // if hash is not in memory, summarize file
                string summary = await Summarize(path, hashString, content, cancellationToken);
                // store hash in memory
                _shortTermMemoryService.Memorize(path, hashString, summary);
            }
            else
            {
                _logger.LogInformation($"File {path} has not changed, skipping");
            }
        }

        private async Task<string> Summarize(PathTo path, string hashString, string content, CancellationToken cancellationToken)
        {
            TikToken tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
            string fileName = Path.GetFileName(path.AbsolutePath);
            string summarizePrompt = SummarizeFilePrompt.SummarizePrompt(fileName, content);
            var summarizePromptTokens = tikToken.Encode(summarizePrompt);

            if (summarizePromptTokens.Count > 4000)
            {
                content = content.Substring(0, content.Length - 1000); // TODO arbirarily shaving off 1000 characters, should be smarter
                return await Summarize(path, hashString, content, cancellationToken);
            }

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 4000 - summarizePromptTokens.Count,
                Temperature = 0.2f,
                N = 1,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(summarizePrompt),
                }
            };

            _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");

            var chatResponse = await _retryPolicy
            .ExecuteAsync(async () => await _openAIService.ChatCompletion
                .CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken)
            );

            if (!chatResponse.Successful)
            {
                throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
            }

            return chatResponse.Choices[0].Message.Content;
        }
    }
}
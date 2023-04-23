using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Tarik.Application.Common;
using TiktokenSharp;
using IFileService = Tarik.Application.Common.IFileService;

namespace Tarik.Application.Brain;

public class PullRequestReviewCommand : IRequest<Unit>
{
    public PullRequestReviewCommand(ReviewPullRequest pullRequest, IFileService fileService)
    {
        PullRequest = pullRequest;
        FileService = fileService;
    }

    public ReviewPullRequest PullRequest { get; }
    public IFileService FileService { get; }

    public class PullRequestReviewCommandHandler : IRequestHandler<PullRequestReviewCommand>
    {
        private readonly ISender _mediator;
        private readonly IPullRequestService _pullRequestService;
        private readonly IOpenAIService _openAIService;
        private readonly IShortTermMemoryService _shortTermMemoryService;
        private readonly ILogger<PullRequestReviewCommandHandler> _logger;

        public PullRequestReviewCommandHandler(
            ISender mediator,
            IPullRequestService pullRequestService,
            IOpenAIService openAIService,
            IShortTermMemoryService shortTermMemoryService,
            ILogger<PullRequestReviewCommandHandler> logger)
        {
            _mediator = mediator;
            _pullRequestService = pullRequestService;
            _openAIService = openAIService;
            _shortTermMemoryService = shortTermMemoryService;
            _logger = logger;
        }

        public async Task<Unit> Handle(PullRequestReviewCommand request, CancellationToken cancellationToken)
        {
            if (request.PullRequest.Comments.Count == 0)
            {
                _logger.LogInformation("No comments on PR #{id}: {title}, skipping", request.PullRequest.Id, request.PullRequest.Title);
                return Unit.Value;
            }

            foreach (var comment in request.PullRequest.Comments)
            {
                _logger.LogInformation("Processing comment {commentId} on PR #{id}: {title}", comment.Id, request.PullRequest.Id, request.PullRequest.Title);

                // TODO - collect comment threads into one request
                await RespondToComment(request, comment, cancellationToken);
            }

            return Unit.Value;
        }

        private async Task RespondToComment(PullRequestReviewCommand request, ReviewComment comment, CancellationToken cancellationToken)
        {
            var prompt = await RespondToReviewCommentPrompts.StateIntentPrompt(
                comment,
                request.FileService,
                _shortTermMemoryService.Dump(request.PullRequest.RepositoryOwner, request.PullRequest.RepositoryName),
                cancellationToken);

            _logger.LogDebug($"Prompt: {prompt}");

            TikToken tikToken = TikToken.EncodingForModel("gpt-4");
            int promptTokenLength = tikToken.Encode(prompt).Count;

            _logger.LogDebug($"Prompt token length: {promptTokenLength} (max 8000)");

            ChatCompletionCreateRequest chatCompletionCreateRequest = new()
            {
                Temperature = 0f,
                N = 1,
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 4000 - promptTokenLength,
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(prompt),
                }
            };

            var response = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken);
            var content = response.Choices[0].Message.Content;

            var intent = content.Deserialize<ReviewCommentIntent>();
            _logger.LogDebug(intent?.ToString());
        }
    }
}
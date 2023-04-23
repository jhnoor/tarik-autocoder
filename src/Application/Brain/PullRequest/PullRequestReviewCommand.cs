using MediatR;
using Microsoft.Extensions.Logging;
using Tarik.Application.Common;

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
        private readonly ILogger<PullRequestReviewCommandHandler> _logger;

        public PullRequestReviewCommandHandler(
            ISender mediator,
            IPullRequestService pullRequestService,
            ILogger<PullRequestReviewCommandHandler> logger)
        {
            _mediator = mediator;
            _pullRequestService = pullRequestService;
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

                await HandleComment(request, comment, cancellationToken);
            }

            return Unit.Value;
        }

        private async Task HandleComment(PullRequestReviewCommand request, ReviewComment comment, CancellationToken cancellationToken)
        {

        }
    }
}
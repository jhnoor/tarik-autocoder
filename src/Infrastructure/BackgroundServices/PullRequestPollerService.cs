using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarik.Application.Brain;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class PullRequestPollerService : BackgroundService
{
    private readonly ILogger<PullRequestPollerService> _logger;
    private readonly IPullRequestService _pullRequestService;
    private readonly IWorkItemService _workItemService;
    private readonly IFileServiceFactory _fileServiceFactory;
    private readonly ISender _mediator;
    private readonly TimeSpan _pollingInterval;

    public PullRequestPollerService(
        ILogger<PullRequestPollerService> logger,
        IFileServiceFactory fileServiceFactory,
        IPullRequestService pullRequestService,
        IWorkItemService workItemService,
        IOptions<AppSettings> appSettings,
        ISender mediator)
    {
        _logger = logger;
        _fileServiceFactory = fileServiceFactory;
        _pullRequestService = pullRequestService;
        _workItemService = workItemService;
        _mediator = mediator;
        if (appSettings.Value.WorkItemPollingIntervalInMinutes == null)
        {
            throw new ArgumentNullException(nameof(appSettings.Value.WorkItemPollingIntervalInMinutes));
        }

        _pollingInterval = TimeSpan.FromMinutes(appSettings.Value.WorkItemPollingIntervalInMinutes.Value);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Polling for assigned pull requests...");

                var pullRequests = await _pullRequestService.GetPrsAssignedToTarikForReview(cancellationToken);

                foreach (var pr in pullRequests)
                {
                    using IFileService fileService = _fileServiceFactory.CreateFileService(pr);

                    _logger.LogInformation($"Reviewing my pr: #{pr.Id}: {pr.Title}.");
                    await _mediator.Send(new PullRequestReviewCommand(pr, fileService), cancellationToken);
                    // TODO state machine for prs
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling for assigned work items.");
            }

            _logger.LogInformation($"Waiting for {_pollingInterval.TotalMinutes} minutes before polling again.");

            // Wait for the configured polling interval before polling again
            await Task.Delay(_pollingInterval, cancellationToken);
        }
    }
}
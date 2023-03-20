using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarik.Application.Common;
using Tarik.Application.CQRS;

namespace Tarik.Infrastructure;

public class WorkItemPollerService : BackgroundService
{
    private readonly ILogger<WorkItemPollerService> _logger;
    private readonly IWorkItemApiClient _workItemApiClient;
    private readonly ISender _mediator;
    private readonly TimeSpan _pollingInterval;

    public WorkItemPollerService(ILogger<WorkItemPollerService> logger, IWorkItemApiClient workItemApiClient, IOptions<AppSettings> appSettings, ISender mediator)
    {
        _logger = logger;
        _workItemApiClient = workItemApiClient;
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
                _logger.LogInformation("Polling for assigned work items...");

                var workItems = await _workItemApiClient.GetOpenWorkItems(cancellationToken);

                foreach (var workItem in workItems)
                {
                    _logger.LogInformation($"Processing work item: #{workItem.Id}: {workItem.Title}. Labels: {string.Join(", ", workItem.Labels)}");
                    await _mediator.Send(new WorkCommand(workItem), cancellationToken);
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
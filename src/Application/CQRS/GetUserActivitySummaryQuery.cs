using MediatR;
using Microsoft.VisualStudio.Services.Profile;
using Tarik.Application.Common;
using Tarik.Application.Common.DTOs;

namespace Tarik.Application.CQRS;

public class GetUserActivitySummaryQuery : IRequest<UserActivitySummaryDTO>
{
    public GetUserActivitySummaryQuery(GetDailyDigestDTO dailyDigestDTO)
    {
        DailyDigestDTO = dailyDigestDTO;
    }

    public GetDailyDigestDTO DailyDigestDTO { get; }

    public class GetUserActivitySummaryQueryHandler : IRequestHandler<GetUserActivitySummaryQuery, UserActivitySummaryDTO>
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly IDateTimeService _dateTimeService;

        public GetUserActivitySummaryQueryHandler(IAzureDevOpsService azureDevOpsService, IDateTimeService dateTimeService)
        {
            _azureDevOpsService = azureDevOpsService;
            _dateTimeService = dateTimeService;
        }

        public async Task<UserActivitySummaryDTO> Handle(GetUserActivitySummaryQuery request, CancellationToken cancellationToken)
        {
            if (request.DailyDigestDTO.PersonalAccessToken == null || request.DailyDigestDTO.UserEmail == null || request.DailyDigestDTO.AzureDevOpsUrl == null || request.DailyDigestDTO.ProjectName == null)
            {
                throw new ArgumentNullException("Some of the required parameters are null."); // TODO - code smell
            }

            (DateTime fromDate, DateTime toDate) = _dateTimeService.GetFromDateAndToDate(request.DailyDigestDTO.FromDate, request.DailyDigestDTO.ToDate);

            Profile user = await _azureDevOpsService.GetUser(
                request.DailyDigestDTO.PersonalAccessToken,
                request.DailyDigestDTO.AzureDevOpsUrl,
                request.DailyDigestDTO.UserEmail,
                cancellationToken
            );

            List<GitCommitDTO> gitCommits = await _azureDevOpsService.GetAllGitCommitsForUser(
                request.DailyDigestDTO.PersonalAccessToken,
                request.DailyDigestDTO.AzureDevOpsUrl,
                request.DailyDigestDTO.ProjectName,
                request.DailyDigestDTO.UserEmail,
                fromDate,
                toDate,
                cancellationToken
            );

            List<WorkItemUpdateDTO> workItemUpdates = await _azureDevOpsService.GetAllWorkItemUpdatesForUser(
                request.DailyDigestDTO.PersonalAccessToken,
                request.DailyDigestDTO.AzureDevOpsUrl,
                request.DailyDigestDTO.ProjectName,
                request.DailyDigestDTO.UserEmail,
                fromDate,
                toDate,
                cancellationToken
            );

            List<PullRequestDTO> pullRequests = await _azureDevOpsService.GetAllPullRequestsCompletedByUser(
                request.DailyDigestDTO.PersonalAccessToken,
                request.DailyDigestDTO.AzureDevOpsUrl,
                request.DailyDigestDTO.ProjectName,
                request.DailyDigestDTO.UserEmail,
                user.Id,
                fromDate,
                toDate,
                cancellationToken
            );

            var userActivitySummary = new UserActivitySummaryDTO(
                request.DailyDigestDTO.UserEmail,
                fromDate,
                toDate,
                gitCommits.Count,
                workItemUpdates.Count,
                pullRequests.Count,
                gitCommits,
                workItemUpdates,
                pullRequests
            );

            return userActivitySummary;
        }
    }
}
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Profile.Client;

namespace Tarik.Application.Common;

public interface IAzureDevOpsHttpClientFactory
{
    ProfileHttpClient CreateProfileHttpClient(string personalAccessToken, string azureDevOpsUrl);
    GitHttpClient CreateGitHttpClient(string personalAccessToken, string azureDevOpsUrl);
    WorkItemTrackingHttpClient CreateWorkItemTrackingHttpClient(string personalAccessToken, string azureDevOpsUrl);
}
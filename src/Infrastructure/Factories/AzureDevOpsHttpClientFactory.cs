using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Profile.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Tarik.Application.Common;

namespace Tarik.Infrastructure.Factories;

public class AzureDevOpsHttpClientFactory : IAzureDevOpsHttpClientFactory
{
    private GitHttpClient? _gitHttpClient;
    private WorkItemTrackingHttpClient? _workItemTrackingHttpClient;
    private ProfileHttpClient? _profileHttpClient;

    public ProfileHttpClient CreateProfileHttpClient(string personalAccessToken, string azureDevOpsUrl)
    {
        if (_profileHttpClient != null)
        {
            return _profileHttpClient;
        }

        var connection = new VssConnection(new Uri(azureDevOpsUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        _profileHttpClient = connection.GetClient<ProfileHttpClient>();
        return _profileHttpClient;
    }

    public GitHttpClient CreateGitHttpClient(string personalAccessToken, string azureDevOpsUrl)
    {
        if (_gitHttpClient != null)
        {
            return _gitHttpClient;
        }

        var connection = new VssConnection(new Uri(azureDevOpsUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        _gitHttpClient = connection.GetClient<GitHttpClient>(); ;
        return _gitHttpClient;
    }

    public WorkItemTrackingHttpClient CreateWorkItemTrackingHttpClient(string personalAccessToken, string azureDevOpsUrl)
    {
        if (_workItemTrackingHttpClient != null)
        {
            return _workItemTrackingHttpClient;
        }

        var connection = new VssConnection(new Uri(azureDevOpsUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        _workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();
        return _workItemTrackingHttpClient;
    }
}
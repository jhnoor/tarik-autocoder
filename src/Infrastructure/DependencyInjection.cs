using OpenAI.GPT3.Extensions;
using Tarik.Application.Common;
using Tarik.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddTransient<IWorkItemService, GitHubWorkItemService>();
        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IFileService, FileService>();
        services.AddTransient<IGitHubClientFactory, GitHubClientFactory>();
        services.AddTransient<IPullRequestService, PullRequestService>();

        services.AddOpenAIService();

        services.AddHostedService<WorkItemPollerService>();

        return services;
    }
}

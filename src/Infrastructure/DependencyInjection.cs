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
        services.AddTransient<IGitHubClientFactory, GitHubClientFactory>();
        services.AddTransient<IPullRequestService, PullRequestService>();
        services.AddTransient<IFileServiceFactory, FileServiceFactory>();
        services.AddTransient<IShellCommandService, ShellCommandService>();

        services.AddSingleton<IShortTermMemoryService, ShortTermMemoryService>();

        services.AddOpenAIService().ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(3));

        services.AddHostedService<WorkItemPollerService>();

        return services;
    }
}

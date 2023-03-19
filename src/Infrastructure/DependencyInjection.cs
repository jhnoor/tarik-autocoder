using OpenAI.GPT3.Extensions;
using Tarik.Application.Common;
using Tarik.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddTransient<IWorkItemApiClient, GitHubIssuesApiClient>();
        services.AddTransient<IDateTimeService, DateTimeService>();

        services.AddOpenAIService();

        services.AddHostedService<WorkItemPollerService>();

        return services;
    }
}

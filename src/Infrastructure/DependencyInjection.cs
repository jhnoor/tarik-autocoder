using Microsoft.Extensions.Configuration;
using OpenAI.GPT3.Extensions;
using Tarik.Application.Common;
using Tarik.Infrastructure.Factories;
using Tarik.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAzureDevOpsService, AzureDevOpsService>();
        services.AddTransient<IDateTimeService, DateTimeService>();

        services.AddScoped<IAzureDevOpsHttpClientFactory, AzureDevOpsHttpClientFactory>();

        services.AddOpenAIService();

        return services;
    }
}

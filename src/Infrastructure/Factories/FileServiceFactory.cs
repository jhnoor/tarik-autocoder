using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class FileServiceFactory : IFileServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _gitHubPAT;

    public FileServiceFactory(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings)
    {
        _serviceProvider = serviceProvider;
        if (string.IsNullOrEmpty(appSettings.Value.GitHubPAT))
        {
            throw new ArgumentNullException(nameof(AppSettings.GitHubPAT));
        }

        _gitHubPAT = appSettings.Value.GitHubPAT;
    }

    public IFileService CreateFileService(WorkItem workItem)
    {
        return new FileService(
            _gitHubPAT,
            workItem,
            _serviceProvider.GetRequiredService<IGitHubClientFactory>(),
            _serviceProvider.GetRequiredService<IShellCommandService>(),
            _serviceProvider.GetRequiredService<IShortTermMemoryService>(),
            _serviceProvider.GetRequiredService<ILogger<IFileService>>());
    }
}
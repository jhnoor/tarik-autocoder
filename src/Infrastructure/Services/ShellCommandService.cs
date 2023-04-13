using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Tarik.Application.Common;

namespace Tarik.Infrastructure;

public class ShellCommandService : IShellCommandService
{
    private readonly ILogger<IShellCommandService> _logger;

    public ShellCommandService(ILogger<IShellCommandService> logger)
    {
        _logger = logger;
    }

    public Task ExecuteCommand(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        return ExecuteCommand<object>(command, arguments, workingDirectory, null, cancellationToken: cancellationToken);
    }

    public async Task<T?> ExecuteCommand<T>(string command, string arguments, string workingDirectory, Func<string, T>? parseOutput, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using Process process = new Process { StartInfo = startInfo };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            return parseOutput != null ? parseOutput(output) : default;
        }
        else
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new ShellCommandException($"Command {command} with arguments {arguments} failed, exit code: {process.ExitCode}. Error: {error}");
        }
    }
}

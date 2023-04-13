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

    public async Task ExecuteCommand(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogDebug(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogError(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            return;
        }
        else
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Command {command} in {workingDirectory} with arguments {arguments} failed, exit code: {process.ExitCode}. Error: {error}");
        }
    }
}

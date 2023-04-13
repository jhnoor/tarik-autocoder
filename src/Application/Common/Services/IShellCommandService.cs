namespace Tarik.Application.Common;

public interface IShellCommandService
{
    Task ExecuteCommand(string command, string arguments, string workingDirectory, CancellationToken cancellationToken);
    Task<T?> ExecuteCommand<T>(string command, string arguments, string workingDirectory, Func<string, T>? parseOutput, CancellationToken cancellationToken);
}
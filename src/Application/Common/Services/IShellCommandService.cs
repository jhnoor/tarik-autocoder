namespace Tarik.Application.Common;

public interface IShellCommandService
{
    Task ExecuteCommand(string command, string arguments, string workingDirectory, CancellationToken cancellationToken);
}
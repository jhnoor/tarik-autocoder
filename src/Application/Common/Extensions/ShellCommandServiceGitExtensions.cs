namespace Tarik.Application.Common;

public static class ShellCommandServiceGitExtensions
{
    public static Task GitClone(this IShellCommandService shellCommandService, string repositoryWithPATUrl, string workingDirectory, CancellationToken cancellationToken)
    {
        // parent directory must be working directory for git clone
        var parentDirectory = Path.GetDirectoryName(workingDirectory);
        if (parentDirectory == null)
        {
            throw new InvalidOperationException($"Could not get parent directory of {workingDirectory}");
        }

        return shellCommandService.ExecuteCommand("git", $"clone --depth 1 {repositoryWithPATUrl} {workingDirectory}", parentDirectory, cancellationToken);
    }

    public static async Task GitCheckout(this IShellCommandService shellCommandService, string branchName, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            await shellCommandService.ExecuteCommand("git", $"checkout -b {branchName}", workingDirectory, cancellationToken);
        }
        catch (ShellCommandException e)
        {
            if (e.Message.Contains("already exists"))
            {
                await shellCommandService.ExecuteCommand("git", $"checkout {branchName}", workingDirectory, cancellationToken);
            }
            else
            {
                throw;
            }
        }
    }

    public static Task GitAddAll(this IShellCommandService shellCommandService, string workingDirectory, CancellationToken cancellationToken)
    {
        return shellCommandService.ExecuteCommand("git", $"add --all", workingDirectory, cancellationToken);
    }

    public static Task GitCommit(this IShellCommandService shellCommandService, string message, string workingDirectory, CancellationToken cancellationToken)
    {
        return shellCommandService.ExecuteCommand("git", $"commit -m \"{message}\"", workingDirectory, cancellationToken);
    }

    public static Task GitPush(this IShellCommandService shellCommandService, string branchName, string workingDirectory, CancellationToken cancellationToken)
    {
        return shellCommandService.ExecuteCommand("git", $"push origin {branchName}", workingDirectory, cancellationToken);
    }

    public static async Task GitConfig(this IShellCommandService shellCommandService, string name, string email, string workingDirectory, CancellationToken cancellationToken)
    {
        await shellCommandService.ExecuteCommand("git", $"config user.name \"{name}\"", workingDirectory, cancellationToken);
        await shellCommandService.ExecuteCommand("git", $"config user.email \"{email}\"", workingDirectory, cancellationToken);
        await shellCommandService.ExecuteCommand("git", $"config push.autoSetupRemote true", workingDirectory, cancellationToken);
    }

    public static async Task<string> GitBranchName(this IShellCommandService shellCommandService, string workingDirectory, CancellationToken cancellationToken)
    {
        string? branchName = await shellCommandService.ExecuteCommand<string>("git", $"branch --show-current", workingDirectory, ParseGitBranchName, cancellationToken);
        if (branchName == null)
        {
            throw new InvalidOperationException("Could not get branch name");
        }
        return branchName;
    }

    public static string ParseGitBranchName(string gitBranchNameOutput)
    {
        var lines = gitBranchNameOutput.Split(Environment.NewLine);
        var branchName = lines[0].Trim();
        return branchName;
    }
}
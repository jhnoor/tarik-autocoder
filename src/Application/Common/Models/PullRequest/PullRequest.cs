using Octokit;

namespace Tarik.Application.Common;

public class PullRequest
{
    public bool Open { get; }

    public PullRequest(Octokit.PullRequest pr)
    {
        Open = pr.State == ItemState.Open;
    }
}
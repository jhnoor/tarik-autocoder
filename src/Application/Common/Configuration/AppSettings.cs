namespace Tarik.Application.Common;

public class AppSettings
{
    public int? WorkItemPollingIntervalInMinutes { get; set; }
    public GitHubSettings? GitHub { get; set; }
}

public class GitHubSettings
{
    public string? PAT { get; set; }
    public string? Owner { get; set; }
    public string? Repo { get; set; }
}
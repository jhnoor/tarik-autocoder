namespace Tarik.Application.Common.DTOs;

public class GitCommitDTO
{
    public string RepositoryName { get; }
    public string CommitId { get; }
    public DateTime Date { get; }
    public string Message { get; }

    public GitCommitDTO(string repositoryName, string commitId, DateTime date, string message)
    {
        RepositoryName = repositoryName;
        CommitId = commitId;
        Date = date;
        Message = message;
    }
}
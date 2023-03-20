using Octokit;

namespace Tarik.Application.Common.DTOs;

public class CommentDTO
{
    public int Id { get; }
    public string Body { get; }
    public bool IsLiked { get; }
    public bool IsTarik { get; }
    public bool IsApprovedPlan { get; }
    public string Url { get; }

    public CommentDTO(IssueComment comment, User tarikUser)
    {
        Id = comment.Id;
        IsTarik = comment.User.Id == tarikUser.Id;
        Body = comment.Body;
        IsLiked = (comment.Reactions.Plus1 + comment.Reactions.Heart + comment.Reactions.Hooray) > 0;
        Url = comment.HtmlUrl;
        IsApprovedPlan = IsLiked && IsTarik && Body.Contains("Plan approved! ✅");
    }
}
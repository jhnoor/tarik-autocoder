namespace Tarik.Application.Common;

public class ReviewComment
{
    public int Id { get; }
    public string Body { get; }
    public int? Line { get; }
    private readonly string _relativePath;

    public ReviewComment(Octokit.PullRequestReviewComment reviewComment)
    {
        Id = reviewComment.Id;
        Body = reviewComment.Body;
        Line = reviewComment.Position;
        _relativePath = reviewComment.Path;
    }

    public PathTo PathTo(IFileService fileService)
    {
        return new PathTo(_relativePath, fileService.LocalDirectory(), true);
    }
}
namespace Tarik.Application.Common;

public abstract class PlanStep
{
    public PathTo PathTo { get; }

    public PlanStep(string path, string localDirectory)
    {
        PathTo = new PathTo(path, localDirectory, isRelative: true);
    }
}

public abstract class MutateFilePlanStep : PlanStep
{
    protected MutateFilePlanStep(string path, string localDirectory, string reason, List<string> relevantFilePaths) : base(path: path, localDirectory: localDirectory)
    {
        Reason = reason;
        RelevantFiles = relevantFilePaths.Select(x => new PathTo(x, localDirectory, isRelative: true)).ToList();
    }

    public string Reason { get; }
    public string? AISuggestedContent { get; set; }
    public List<PathTo> RelevantFiles { get; }

    public string Dump() => $"* `{PathTo.RelativePath}` | {Reason} | [{string.Join(", ", RelevantFiles.Select(x => x.RelativePath))}]";
}

public class CreateFilePlanStep : MutateFilePlanStep
{
    public CreateFilePlanStep(string path, string localDirectory, string reason, List<string> relevantFilePaths) : base(
        path: path,
        localDirectory: localDirectory,
        reason: reason,
        relevantFilePaths: relevantFilePaths)
    {
    }
}

public class EditFilePlanStep : MutateFilePlanStep
{
    public EditFilePlanStep(string path, string localDirectory, string reason, List<string> relevantFilePaths) : base(
        path: path,
        localDirectory: localDirectory,
        reason: reason,
        relevantFilePaths: relevantFilePaths)
    {
    }

    public string? CurrentContent { get; set; }
}

public class DeleteFilePlanStep : PlanStep
{
    public DeleteFilePlanStep(string path, string localDirectory) : base(path: path, localDirectory: localDirectory)
    {
    }
}
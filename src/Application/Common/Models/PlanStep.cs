namespace Tarik.Application.Common;

public abstract class PlanStep
{
    public PathTo PathTo { get; }

    public PlanStep(string path, string localDirectory)
    {
        PathTo = new PathTo(Path.Combine(localDirectory, path), localDirectory); // TODO code smell
    }
}

public abstract class MutateFilePlanStep : PlanStep
{
    protected MutateFilePlanStep(string path, string localDirectory, string reason) : base(path: path, localDirectory: localDirectory)
    {
        Reason = reason;
    }

    public string Reason { get; }
    public string? AISuggestedContent { get; set; }
}

public class CreateFilePlanStep : MutateFilePlanStep
{
    public CreateFilePlanStep(string path, string localDirectory, string reason) : base(path: path, localDirectory: localDirectory, reason: reason)
    {
    }
}

public class EditFilePlanStep : MutateFilePlanStep
{
    public EditFilePlanStep(string path, string localDirectory, string reason) : base(path: path, localDirectory: localDirectory, reason: reason)
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
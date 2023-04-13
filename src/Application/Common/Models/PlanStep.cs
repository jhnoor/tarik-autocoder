namespace Tarik.Application.Common;

public abstract class PlanStep
{
    public string Path { get; }
    public string Reason { get; }

    public PlanStep(string path, string reason)
    {
        Path = path;
        Reason = reason;
    }
}

public abstract class MutateFilePlanStep : PlanStep
{
    protected MutateFilePlanStep(string path, string reason) : base(path, reason)
    {
    }

    public string? AISuggestedContent { get; set; }
}

public class CreateFilePlanStep : MutateFilePlanStep
{
    public CreateFilePlanStep(string path, string reason) : base(path, reason)
    {
    }
}

public class EditFilePlanStep : MutateFilePlanStep
{
    public EditFilePlanStep(string path, string reason) : base(path, reason)
    {
    }

    public string? CurrentContent { get; set; }
}

public class DeleteFilePlanStep : PlanStep
{
    public DeleteFilePlanStep(string path, string reason) : base(path, reason)
    {
    }
}
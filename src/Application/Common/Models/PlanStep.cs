namespace Tarik.Application.Common;

public abstract class PlanStep
{
    public string? Path { get; set; }
    public string? Reason { get; set; }
}

public abstract class MutateFilePlanStep : PlanStep
{
    public string? AISuggestedContent { get; set; }
}

public class CreateFilePlanStep : MutateFilePlanStep { }

public class EditFilePlanStep : MutateFilePlanStep
{
    public string CurrentContent { get; set; } = string.Empty;
}

public class DeleteFilePlanStep : PlanStep { }
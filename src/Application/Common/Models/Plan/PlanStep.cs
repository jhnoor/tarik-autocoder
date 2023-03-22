namespace Tarik.Application.Common;

public abstract class PlanStep
{
    public string? Path { get; set; }
    public string? Reason { get; set; }
}

public class CreateFilePlanStep : PlanStep
{
    public string? Content { get; set; }
}

public class EditFilePlanStep : PlanStep
{
    public string? AISuggestedContent { get; set; }
    public string CurrentContent { get; set; } = string.Empty;
}

public class DeleteFilePlanStep : PlanStep
{
}
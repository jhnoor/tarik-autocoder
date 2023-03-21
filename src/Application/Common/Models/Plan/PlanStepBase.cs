namespace Tarik.Application.Common;

public abstract class PlanStepBase
{
    public virtual StepType StepType { get; set; }
    public string? Path { get; set; }
    public string? Reason { get; set; }
}
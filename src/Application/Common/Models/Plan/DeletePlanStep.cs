namespace Tarik.Application.Common;

public class DeleteFilePlanStep : PlanStepBase
{
    public override StepType StepType { get; set; } = StepType.DeleteFile;
}